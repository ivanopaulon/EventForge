#!/usr/bin/env python3
"""Phase 3d - Replace static Style= on Mud* components with CSS classes."""

import re
import sys
from pathlib import Path

ROOT = Path("/home/runner/work/EventForge/EventForge/EventForge.Client")


def normalize_style(s: str) -> str:
    """Normalize a CSS style string for comparison."""
    s = s.strip().rstrip(';').rstrip()
    parts = [p.strip() for p in s.split(';')]
    parts = [p for p in parts if p]
    normalized = []
    for part in parts:
        if ':' in part:
            prop, _, val = part.partition(':')
            normalized.append(f"{prop.strip()}: {val.strip()}")
        else:
            normalized.append(part.strip())
    return '; '.join(normalized)


# Patterns: (component, normalized_style, class_to_add, keep_style)
# keep_style: if not None, keep Style= with this value instead of removing it
# class_to_add: if "__COLOR_INHERIT__", add Color="Color.Inherit" param instead
RAW_PATTERNS = {
    "MudPaper": [
        ("background-color: var(--mud-palette-background-grey)", "bg-grey", None),
        ("background: var(--mud-palette-background-grey)", "bg-grey", None),
        ("border: 1px solid var(--mud-palette-lines-default)", "border-default", None),
        ("border-radius: 12px", "rounded-12", None),
        ("text-align: center", "text-center", None),
        ("background-color: var(--mud-palette-warning-lighten)", "bg-warning-light", None),
        ("background-color: var(--mud-palette-success-lighten)", "bg-success-light", None),
        ("background: var(--mud-palette-success-lighten)", "bg-success-light", None),
        ("background: var(--mud-palette-info-lighten)", "bg-info-light", None),
        ("height:100%; min-height:280px", "h-full-min-280", None),
        ("text-align: center; background: white", "text-center bg-white", None),
        ("border-left: 4px solid var(--mud-palette-primary)", "border-primary-l", None),
        ("border-left: 4px solid var(--mud-palette-secondary)", "border-secondary-l", None),
        ("border-left: 4px solid var(--mud-palette-success)", "border-success-l", None),
        ("border: 1px solid rgba(0,0,0,0.12); border-radius:4px; min-width: 150px", "ef-popup-card", None),
    ],
    "MudStack": [
        ("width: 100%", "w-full", None),
        ("flex: 1; min-width: 0", "flex-1 min-w-0", None),
        ("flex: 1", "flex-1", None),
        ("flex-wrap: wrap", "flex-wrap-wrap", None),
        ("max-width: 300px", "ef-max-300", None),
        ("gap:8px", "gap-2", None),
        ("min-width: 80px", "min-width-80", None),
    ],
    "MudDivider": [
        ("background: rgba(255,255,255,0.3); margin: 8px 0", "divider-dark-bg my-2", None),
        ("border-color: rgba(255,255,255,0.12)", "divider-dark", None),
        ("background: rgba(255,255,255,0.3)", "divider-dark-bg", None),
    ],
    "MudIconButton": [
        ("color: white", "__COLOR_INHERIT__", None),
        ("min-width: 44px; height: 44px", "touch-44", None),
        ("min-width: 52px; height: 52px", "touch-52", None),
        ("margin-right:4px;flex-shrink:0", "mr-1 flex-no-shrink", None),
        ("margin-top: 4px", "mt-1-em", None),
        ("color:rgba(255,255,255,0.70); flex-shrink:0", "icon-btn-muted", None),
    ],
    "MudButton": [
        ("height: 80px; text-align: left; justify-content: flex-start; padding: 16px", "ef-card-btn", None),
        ("align-self: flex-start; margin-top: 4px", "align-self-start mt-1-em", None),
        ("min-width: 32px; height: 32px; padding: 2px", "ef-compact-btn", None),
        ("min-width:32px; height:32px; padding:2px", "ef-compact-btn", None),
        ("min-width: 56px; font-weight: 600", "fw-semibold", "min-width: 56px;"),
    ],
    "MudNumericField": [
        ("width: 60px; text-align: center", "ef-input-60 text-center", None),
        ("max-width: 100px; text-align: center; font-size: 1.3rem; font-weight: 700",
         None, None),  # too complex, skip
        ("max-width:100px", "ef-input-100", None),
        ("max-width: 100px", "ef-input-100", None),
        ("width: 120px", "ef-input-120", None),
        ("width:120px", "ef-input-120", None),
        ("width: 150px", "ef-input-150", None),
        ("width:100px", "ef-input-100", None),
        ("width: 100px", "ef-input-100", None),
        ("width: 60px", "ef-input-60", None),
        ("font-size: 1.1rem", "text-lg-custom", None),
    ],
    "MudSelect": [
        ("min-width: 150px; margin-right: 8px", "min-width-150 mr-2", None),
        ("min-width: 200px; margin-right: 8px", "ef-min-200 mr-2", None),
        ("min-width: 150px", "min-width-150", None),
        ("min-width: 200px", "ef-min-200", None),
        ("flex: 1", "flex-1", None),
        # flex: 0 0 150px -> no good class, skip
        # min-width:140px; max-width:180px -> skip (too specific)
        # min-width:120px; max-width:160px -> skip (too specific)
    ],
}

# Build normalized lookup: component -> [(norm_pattern, class_add, keep_style), ...]
PATTERNS: dict = {}
for comp, entries in RAW_PATTERNS.items():
    PATTERNS[comp] = []
    seen_norms = set()
    for (sv, ca, ks) in entries:
        norm = normalize_style(sv)
        if norm not in seen_norms:
            seen_norms.add(norm)
            PATTERNS[comp].append((norm, ca, ks))


def find_tag_end(content: str, pos: int) -> tuple[int, bool]:
    """
    Starting from pos (after component name), find end of Razor/HTML tag.
    Returns (end_pos, is_self_closing) where end_pos is index of > (or last char of />).
    Handles: double-quoted strings (with @() inside), single-quoted strings, @() exprs.
    """
    i = pos
    n = len(content)
    while i < n:
        c = content[i]
        if c == '"':
            i += 1
            while i < n:
                cc = content[i]
                if cc == '"':
                    i += 1
                    break
                elif cc == '@' and i + 1 < n and content[i + 1] == '(':
                    i += 2
                    depth = 1
                    while i < n and depth > 0:
                        if content[i] == '(':
                            depth += 1
                        elif content[i] == ')':
                            depth -= 1
                        i += 1
                else:
                    i += 1
        elif c == "'":
            i += 1
            while i < n and content[i] != "'":
                i += 1
            if i < n:
                i += 1
        elif c == '@' and i + 1 < n and content[i + 1] == '(':
            i += 2
            depth = 1
            while i < n and depth > 0:
                if content[i] == '(':
                    depth += 1
                elif content[i] == ')':
                    depth -= 1
                i += 1
        elif c == '/' and i + 1 < n and content[i + 1] == '>':
            return i + 1, True
        elif c == '>':
            return i, False
        else:
            i += 1
    return -1, False


def extract_style_value(tag_text: str) -> tuple[str, str] | None:
    """
    Find Style= attribute in a tag.
    Returns (full_match, inner_value) or None if not found or dynamic.
    """
    # Match Style="..." or Style='...'
    m = re.search(r'\bStyle\s*=\s*"([^"]*)"', tag_text, re.DOTALL)
    if not m:
        m = re.search(r"\bStyle\s*=\s*'([^']*)'", tag_text, re.DOTALL)
    if not m:
        return None
    inner = m.group(1)
    if '@' in inner:
        return None  # dynamic, skip
    return m.group(0), inner


def build_new_tag(tag_text: str, component: str, style_full: str,
                  style_value: str, class_add: str, keep_style: str | None) -> str:
    """Apply a single replacement to a tag and return the new tag text."""

    if class_add == "__COLOR_INHERIT__":
        # Add Color="Color.Inherit" only if Color= not already present
        if re.search(r'\bColor\s*=', tag_text):
            return tag_text
        new_tag = tag_text.replace(style_full, 'Color="Color.Inherit"', 1)
        return new_tag

    # Find existing Class= attribute
    class_match = re.search(r'\bClass\s*=\s*"([^"]*)"', tag_text, re.DOTALL)

    if keep_style is not None:
        # Replace Style= with new shorter Style= and add/merge Class=
        new_style_attr = f'Style="{keep_style}"'
        if class_match:
            existing = class_match.group(1).strip()
            new_class_val = f'{existing} {class_add}'.strip()
            new_tag = tag_text.replace(style_full, new_style_attr, 1)
            new_tag = new_tag.replace(class_match.group(0), f'Class="{new_class_val}"', 1)
        else:
            new_tag = tag_text.replace(style_full, f'{new_style_attr} Class="{class_add}"', 1)
        return new_tag

    # Remove Style= and add/merge Class=
    if class_match:
        existing = class_match.group(1).strip()
        new_class_val = f'{existing} {class_add}'.strip()
        # Remove Style= attribute
        new_tag = remove_attr(tag_text, style_full)
        # Update Class= attribute
        new_tag = new_tag.replace(class_match.group(0), f'Class="{new_class_val}"', 1)
    else:
        # Replace Style= with Class=
        new_tag = tag_text.replace(style_full, f'Class="{class_add}"', 1)

    return new_tag


def remove_attr(tag_text: str, attr_full: str) -> str:
    """Remove an attribute from tag text, cleaning up surrounding whitespace."""
    # Try to also eat the whitespace before or after the attribute
    escaped = re.escape(attr_full)
    # Remove with preceding whitespace (space/tab/newline)
    result = re.sub(r'(?:[ \t]+|(?<=\n))' + escaped, '', tag_text, count=1)
    if result != tag_text:
        return result
    # Try trailing whitespace
    result = re.sub(escaped + r'(?:[ \t]+|(?=\n))', '', tag_text, count=1)
    if result != tag_text:
        return result
    # Fallback: plain replace
    result = tag_text.replace(attr_full, '', 1)
    # Clean up any double spaces
    result = re.sub(r'  +', ' ', result)
    return result


def process_file(filepath: Path) -> int:
    """Process a single Razor file. Returns count of replacements made."""
    content = filepath.read_text(encoding='utf-8')
    components = list(PATTERNS.keys())

    # Collect all replacements as (start, end, new_text)
    # We process greedily, avoiding overlapping replacements
    replacements: list[tuple[int, int, str]] = []
    covered: list[tuple[int, int]] = []  # regions already scheduled for replacement

    for component in components:
        patterns = PATTERNS[component]
        search_str = f'<{component}'

        pos = 0
        while True:
            idx = content.find(search_str, pos)
            if idx == -1:
                break

            # Make sure it's a genuine tag start
            end_name = idx + len(search_str)
            if end_name < len(content) and content[end_name] not in ' \n\r\t/>':
                pos = idx + 1
                continue

            # Find tag end
            tag_end, _ = find_tag_end(content, end_name)
            if tag_end == -1:
                pos = idx + 1
                continue

            tag_text = content[idx:tag_end + 1]

            # Check overlap with already-scheduled replacements
            overlaps = any(not (tag_end < s or idx > e) for s, e, _ in replacements)
            if overlaps:
                pos = tag_end + 1
                continue

            # Extract static Style= value
            result = extract_style_value(tag_text)
            if result is None:
                pos = tag_end + 1
                continue

            style_full, style_inner = result
            norm_value = normalize_style(style_inner)

            # Match against patterns (already sorted longest-first by entry order)
            matched = None
            for (norm_pattern, class_add, keep_style) in patterns:
                if norm_value == norm_pattern:
                    matched = (norm_pattern, class_add, keep_style)
                    break

            if not matched:
                pos = tag_end + 1
                continue

            _, class_add, keep_style = matched

            # Skip "too complex" markers
            if class_add is None:
                pos = tag_end + 1
                continue

            new_tag = build_new_tag(tag_text, component, style_full, style_inner,
                                     class_add, keep_style)

            if new_tag != tag_text:
                replacements.append((idx, tag_end, new_tag))

            pos = tag_end + 1

    if not replacements:
        return 0

    # Apply in reverse order to preserve positions
    replacements.sort(key=lambda x: x[0], reverse=True)
    new_content = content
    for start, end, new_text in replacements:
        new_content = new_content[:start] + new_text + new_content[end + 1:]

    filepath.write_text(new_content, encoding='utf-8')
    return len(replacements)


def main() -> int:
    razor_files = sorted(ROOT.glob("**/*.razor"))
    total = 0
    changed = 0

    for f in razor_files:
        count = process_file(f)
        if count > 0:
            total += count
            changed += 1
            print(f"  {f.relative_to(ROOT)}: {count}")

    print(f"\nTotal: {total} replacements in {changed} files")
    return total


if __name__ == '__main__':
    main()
