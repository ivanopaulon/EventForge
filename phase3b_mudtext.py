#!/usr/bin/env python3
"""
Phase 3b: Replace static Style= attributes on MudText components with CSS classes.
Uses a Razor-aware state-machine parser to handle @() expressions.
"""
import re
import sys
import os
import glob

# ---------------------------------------------------------------------------
# Replacement mapping
# ---------------------------------------------------------------------------
EXACT_MAPPINGS = {
    # Single-value style → class only
    "color: var(--mud-palette-text-disabled);": ("class_add", "text-disabled"),
    "color: var(--mud-palette-text-secondary);": ("class_add", "text-secondary-color"),
    "color: var(--mud-palette-primary);": ("color_param", "Color.Primary"),
    "color: rgba(255,255,255,0.8);": ("class_add", "text-white-80"),
    "color: rgba(255,255,255,0.9);": ("class_add", "text-white-90"),
    "font-family: monospace;": ("class_add", "text-mono"),
    "font-family: monospace; font-size: 0.8rem;": ("class_add", "text-mono-sm"),
    "font-size: 0.65rem;": ("class_add", "text-xxs"),
    "font-size: 0.7rem;": ("class_add", "text-xs"),
    "font-size: 0.8rem;": ("class_add", "text-sm-custom"),
    "font-size: 1.1rem;": ("class_add", "text-lg-custom"),
    "margin-left: 8px;": ("class_add", "ml-2"),

    # Multi-value → class only
    "color: rgba(255,255,255,0.70); margin-top: 4px; font-family: monospace;": ("class_add", "text-code-muted"),
    "color: rgba(255,255,255,0.70); margin-top: 4px; font-family: monospace; word-break: break-word;": ("class_add", "text-code-muted text-breakword"),
    "color: rgba(255,255,255,0.87); margin-top: 4px;": ("class_add", "text-white-87 mt-4-px"),
    "color: rgba(255,255,255,0.8); margin-top: 12px; text-align: center;": ("class_add", "text-white-80 mt-3"),
    "font-family: monospace; color: rgba(255,255,255,0.9); letter-spacing: 2px;": ("class_add", "text-card-mono"),
    "color:rgba(255,255,255,0.87); font-weight:500; flex-grow:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;": ("class_add", "text-header-dark"),
    "overflow:hidden;text-overflow:ellipsis;white-space:nowrap;display:block;": ("class_add", "text-truncate-block"),
    "overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-weight:500;": ("class_add", "text-truncate-ef fw-medium"),
    "max-width: 300px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;": ("class_add", "ef-max-300 text-truncate-ef"),
    "max-width:200px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;": ("class_add", "ef-max-200 text-truncate-ef"),
    "max-width:180px;": ("class_add", "ef-max-180"),
    "font-weight: 600; color: white;": ("class_add", "fw-semibold text-white-full"),
    "font-weight: 700; color: white;": ("class_add", "fw-bold text-white-full"),
    "color: white; font-weight: 600;": ("class_add", "fw-semibold text-white-full"),
    "color: white; font-weight: 700;": ("class_add", "fw-bold text-white-full"),
    "font-weight: 500; color: var(--mud-palette-primary);": ("class_color", "fw-medium", "Color.Primary"),
    "font-weight: 600; color: var(--mud-palette-warning);": ("class_color", "fw-semibold", "Color.Warning"),
    "font-weight: 700; color: var(--mud-palette-primary);": ("class_color", "fw-bold", "Color.Primary"),
    "font-weight: 800; color: var(--mud-palette-primary);": ("class_color", "fw-extrabold", "Color.Primary"),
    "font-weight:700; color: var(--mud-palette-success);": ("class_color", "fw-bold", "Color.Success"),
    "font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;": ("class_add", "fw-semibold text-truncate-ef"),
    "font-weight:600; cursor:pointer;": ("class_add", "fw-semibold cursor-pointer"),
    "font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:rgba(255,255,255,0.87);": ("class_add", "fw-semibold text-truncate-ef text-white-87"),
    "font-weight: 600; text-align: center;": ("class_align", "fw-semibold", "Align.Center"),
    "flex: 1; text-align: center; font-style: italic;": ("class_add", "text-italic-centered"),
    "font-weight: 700; min-width: 80px; text-align: right; font-family: monospace;": ("class_align", "fw-bold text-mono-right", "Align.Right"),
    # Special: keep flex-shrink and margin-right as Style
    "flex-shrink:0;font-weight:600;margin-right:4px;": ("class_style", "fw-semibold", "flex-shrink:0; margin-right:4px;"),
    # Special: keep min-width as Style
    "min-width: 36px; text-align: center; font-weight: 700;": ("class_align_style", "fw-bold", "Align.Center", "min-width: 36px;"),
    "opacity: 0.8; color: white;": ("class_add", "opacity-80 text-white-full"),
    "opacity: 0.9; color: white;": ("class_add", "opacity-90 text-white-full"),
    "font-weight: 600; color: #90EE90;": ("class_style", "fw-semibold", "color: #90EE90;"),
    "font-weight: 600; text-align: center; margin-top: 8px;": ("class_align_style", "fw-semibold", "Align.Center", "margin-top: 8px;"),
}


def extract_attr_value(tag_text, start_pos):
    """
    Given tag_text and the position just after the opening quote of an attribute value,
    extract the full value content, handling @() Razor expressions.
    Returns (value_content, end_pos_after_closing_quote).
    """
    # Determine quote char
    quote_char = tag_text[start_pos - 1]
    i = start_pos
    depth = 0  # depth for @() nesting
    result = []
    while i < len(tag_text):
        ch = tag_text[i]
        if depth == 0 and ch == quote_char:
            # End of attribute value
            return ''.join(result), i + 1
        elif ch == '@' and i + 1 < len(tag_text) and tag_text[i + 1] == '(':
            # Razor expression @(...)
            result.append(ch)
            i += 1
            result.append(tag_text[i])
            i += 1
            depth = 1
            while i < len(tag_text) and depth > 0:
                c = tag_text[i]
                if c == '(':
                    depth += 1
                elif c == ')':
                    depth -= 1
                result.append(c)
                i += 1
        else:
            result.append(ch)
            i += 1
    return ''.join(result), i


def find_mudtext_tags(content):
    """
    Find all <MudText ...> opening tags (self-closing or not).
    Returns list of (start, end, tag_text) tuples.
    """
    tags = []
    i = 0
    while i < len(content):
        # Find <MudText
        idx = content.find('<MudText', i)
        if idx == -1:
            break
        # Make sure it's followed by whitespace or > or / (not <MudTextField etc.)
        after = idx + len('<MudText')
        if after < len(content) and (content[after] in ' \t\n\r/>'):
            # Now scan forward to find end of opening tag
            j = after
            in_attr_val = False
            attr_quote = None
            while j < len(content):
                ch = content[j]
                if in_attr_val:
                    if ch == '@' and j + 1 < len(content) and content[j + 1] == '(':
                        # Skip @(...) expression
                        j += 2
                        depth = 1
                        while j < len(content) and depth > 0:
                            if content[j] == '(':
                                depth += 1
                            elif content[j] == ')':
                                depth -= 1
                            j += 1
                        continue
                    elif ch == attr_quote:
                        in_attr_val = False
                        attr_quote = None
                elif ch in ('"', "'"):
                    in_attr_val = True
                    attr_quote = ch
                elif ch == '>':
                    j += 1
                    break
                j += 1
            tag_text = content[idx:j]
            tags.append((idx, j, tag_text))
            i = j
        else:
            i = idx + 1
    return tags


def get_attr(tag_text, attr_name):
    """
    Extract the value of an attribute from a tag text.
    Returns (value, full_match_start, full_match_end) or None.
    """
    pattern = r'\b' + re.escape(attr_name) + r'\s*=\s*'
    m = re.search(pattern, tag_text)
    if not m:
        return None
    pos = m.end()
    if pos >= len(tag_text):
        return None
    quote = tag_text[pos]
    if quote not in ('"', "'"):
        return None
    value, end_pos = extract_attr_value(tag_text, pos + 1)
    return value, m.start(), end_pos


def has_attr(tag_text, attr_name):
    """Check if attribute exists in tag."""
    result = get_attr(tag_text, attr_name)
    return result is not None


def is_dynamic_style(style_val):
    """Return True if the style value contains Razor @() expressions."""
    return '@(' in style_val or style_val.startswith('@')


def normalize_style(s):
    """Normalize style string for matching: strip, collapse spaces."""
    return s.strip()


def transform_tag(tag_text):
    """
    Apply mapping to a MudText opening tag.
    Returns new tag_text (or original if no mapping applies).
    """
    style_result = get_attr(tag_text, 'Style')
    if style_result is None:
        return tag_text

    style_val, style_start, style_end = style_result
    style_norm = normalize_style(style_val)

    # Skip dynamic styles (Razor expressions)
    if is_dynamic_style(style_norm):
        return tag_text

    # Look up in exact mappings
    mapping = EXACT_MAPPINGS.get(style_norm)
    if mapping is None:
        return tag_text

    # Extract current Class and other relevant attrs
    class_result = get_attr(tag_text, 'Class')
    color_result = get_attr(tag_text, 'Color')
    align_result = get_attr(tag_text, 'Align')

    existing_class = class_result[0].strip() if class_result else None
    has_color = color_result is not None
    has_align = align_result is not None

    # Build new tag by removing Style= and potentially Class=, then re-adding
    action = mapping[0]

    # Remove Style= attr from tag
    # We need to remove the full `Style="..."` portion including surrounding whitespace
    # Find the Style attr span in the original tag
    style_attr_pattern = r'\s*\bStyle\s*=\s*["\'][^"\']*["\']'

    # We'll rebuild the tag more carefully
    # First, remove the Style="..." attribute
    def remove_attr(text, attr_name):
        """Remove an attribute from the tag text."""
        pattern = r'(\s+)' + re.escape(attr_name) + r'\s*=\s*"[^"]*"'
        result = re.sub(pattern, '', text)
        if result == text:
            pattern2 = r'(\s+)' + re.escape(attr_name) + r"\s*=\s*'[^']*'"
            result = re.sub(pattern2, '', text)
        return result

    def add_or_merge_class(text, new_classes):
        """Add new_classes to existing Class= or add new Class= attr."""
        class_res = get_attr(text, 'Class')
        if class_res:
            existing, cs, ce = class_res
            # merge
            merged = (existing.strip() + ' ' + new_classes).strip()
            # Replace the Class value
            # Find Class="..." in text and replace its value
            pattern = r'\bClass\s*=\s*"[^"]*"'
            replacement = f'Class="{merged}"'
            return re.sub(pattern, replacement, text, count=1)
        else:
            # Add before the closing > or />
            if text.rstrip().endswith('/>'):
                return text.rstrip()[:-2].rstrip() + f' Class="{new_classes}" />'
            elif text.rstrip().endswith('>'):
                return text.rstrip()[:-1].rstrip() + f' Class="{new_classes}">'
            return text + f' Class="{new_classes}"'

    def add_param(text, param_name, param_value):
        """Add a parameter if it doesn't exist."""
        if has_attr(text, param_name):
            return text
        if text.rstrip().endswith('/>'):
            return text.rstrip()[:-2].rstrip() + f' {param_name}="{param_value}" />'
        elif text.rstrip().endswith('>'):
            return text.rstrip()[:-1].rstrip() + f' {param_name}="{param_value}">'
        return text + f' {param_name}="{param_value}"'

    def set_style(text, new_style):
        """Replace Style= value or add Style=."""
        pattern = r'\bStyle\s*=\s*"[^"]*"'
        if re.search(pattern, text):
            return re.sub(pattern, f'Style="{new_style}"', text, count=1)
        if text.rstrip().endswith('/>'):
            return text.rstrip()[:-2].rstrip() + f' Style="{new_style}" />'
        elif text.rstrip().endswith('>'):
            return text.rstrip()[:-1].rstrip() + f' Style="{new_style}">'
        return text + f' Style="{new_style}"'

    new_tag = tag_text

    if action == 'class_add':
        new_classes = mapping[1]
        new_tag = remove_attr(new_tag, 'Style')
        new_tag = add_or_merge_class(new_tag, new_classes)

    elif action == 'color_param':
        color_val = mapping[1]
        new_tag = remove_attr(new_tag, 'Style')
        if not has_color:
            new_tag = add_param(new_tag, 'Color', color_val)

    elif action == 'class_color':
        new_classes = mapping[1]
        color_val = mapping[2]
        new_tag = remove_attr(new_tag, 'Style')
        new_tag = add_or_merge_class(new_tag, new_classes)
        if not has_color:
            new_tag = add_param(new_tag, 'Color', color_val)

    elif action == 'class_align':
        new_classes = mapping[1]
        align_val = mapping[2]
        new_tag = remove_attr(new_tag, 'Style')
        new_tag = add_or_merge_class(new_tag, new_classes)
        if not has_align:
            new_tag = add_param(new_tag, 'Align', align_val)

    elif action == 'class_style':
        # class_style: remove style, add class, add new style with remaining values
        new_classes = mapping[1]
        remaining_style = mapping[2]
        new_tag = remove_attr(new_tag, 'Style')
        new_tag = add_or_merge_class(new_tag, new_classes)
        new_tag = add_param(new_tag, 'Style', remaining_style)

    elif action == 'class_align_style':
        new_classes = mapping[1]
        align_val = mapping[2]
        remaining_style = mapping[3]
        new_tag = remove_attr(new_tag, 'Style')
        new_tag = add_or_merge_class(new_tag, new_classes)
        if not has_align:
            new_tag = add_param(new_tag, 'Align', align_val)
        new_tag = add_param(new_tag, 'Style', remaining_style)

    return new_tag


def process_file(filepath):
    """Process a single .razor file."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content
    tags = find_mudtext_tags(content)

    # Process in reverse order to preserve positions
    replacements = []
    for start, end, tag_text in tags:
        new_tag = transform_tag(tag_text)
        if new_tag != tag_text:
            replacements.append((start, end, new_tag))

    if not replacements:
        return 0

    # Apply replacements in reverse order
    new_content = content
    for start, end, new_tag in reversed(replacements):
        new_content = new_content[:start] + new_tag + new_content[end:]

    if new_content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(new_content)
        return len(replacements)
    return 0


def main():
    base = '/home/runner/work/EventForge/EventForge/EventForge.Client'
    razor_files = glob.glob(os.path.join(base, '**', '*.razor'), recursive=True)

    total_replaced = 0
    total_files = 0
    for filepath in sorted(razor_files):
        count = process_file(filepath)
        if count:
            rel = os.path.relpath(filepath, base)
            print(f"  {rel}: {count} replacement(s)")
            total_replaced += count
            total_files += 1

    print(f"\nTotal: {total_replaced} replacements in {total_files} files")

    # Count remaining MudText Style= instances
    remaining = 0
    for filepath in razor_files:
        with open(filepath, 'r', encoding='utf-8') as f:
            text = f.read()
        tags = find_mudtext_tags(text)
        for _, _, tag in tags:
            if 'Style=' in tag and not is_dynamic_style_in_tag(tag):
                remaining += 1

    print(f"Remaining static MudText Style= instances: {remaining}")


def is_dynamic_style_in_tag(tag_text):
    """Check if Style in a tag is dynamic."""
    result = get_attr(tag_text, 'Style')
    if result is None:
        return False
    val = result[0]
    return is_dynamic_style(val)


if __name__ == '__main__':
    main()
