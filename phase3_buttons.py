#!/usr/bin/env python3
"""
Phase 3 - Task 4: Add Variant + Color to MudButton instances that are missing Variant.
"""

import re
import sys
from pathlib import Path

CLIENT_DIR = Path("/home/runner/work/EventForge/EventForge/EventForge.Client")


def find_mud_tags(content: str, component_name: str):
    """Find all opening tags for component_name. Returns list of (start, end)."""
    results = []
    i = 0
    n = len(content)
    pattern = f'<{component_name}'

    while i < n:
        idx = content.find(pattern, i)
        if idx == -1:
            break

        after = idx + len(pattern)
        if after < n and content[after] not in ' \t\r\n/>':
            i = idx + 1
            continue

        j = after
        in_string = None
        depth_paren = 0

        while j < n:
            c = content[j]
            if in_string:
                if c == in_string and depth_paren == 0:
                    in_string = None
                elif c == '@' and in_string == '"' and j + 1 < n and content[j + 1] == '(':
                    depth_paren += 1
                    j += 1
                elif c == '(' and depth_paren > 0:
                    depth_paren += 1
                elif c == ')' and depth_paren > 0:
                    depth_paren -= 1
            else:
                if c == '"':
                    in_string = '"'
                elif c == "'":
                    in_string = "'"
                elif c == '>':
                    j += 1
                    results.append((idx, j))
                    break
            j += 1

        i = idx + 1

    return results


def get_attr_value(tag_text: str, attr_name: str) -> str | None:
    """Extract the value of an attribute from tag text."""
    pattern = re.compile(
        re.escape(attr_name) + r'\s*=\s*"([^"]*)"', re.IGNORECASE
    )
    m = pattern.search(tag_text)
    if m:
        return m.group(1)
    return None


def has_attr(tag_text: str, attr_name: str) -> bool:
    """Check if tag has a given attribute."""
    pattern = re.compile(
        r'\b' + re.escape(attr_name) + r'\s*=', re.IGNORECASE
    )
    return bool(pattern.search(tag_text))


def get_button_inner_text(content: str, tag_end: int) -> str:
    """
    Scan forward from tag_end to find button inner text content (non-tag characters).
    Stops at </MudButton>.
    """
    close_tag = '</MudButton>'
    idx = content.find(close_tag, tag_end)
    if idx == -1:
        return ''
    inner = content[tag_end:idx]
    # Strip HTML tags
    inner_text = re.sub(r'<[^>]+>', '', inner)
    return inner_text.strip()


def classify_button(tag_text: str, inner_text: str) -> tuple[str, str]:
    """
    Returns (Variant, Color) based on button content and attributes.
    """
    text_lower = inner_text.lower()
    onclick = get_attr_value(tag_text, 'OnClick') or ''
    onclick_lower = onclick.lower()
    start_icon = get_attr_value(tag_text, 'StartIcon') or ''

    # Cancel / Close
    if any(kw in text_lower for kw in ['cancel', 'annulla', 'chiudi', 'close']) \
            or 'cancel' in onclick_lower:
        return 'Variant.Text', 'Color.Default'

    # Clear / Reset
    if any(kw in text_lower for kw in ['clear', 'reset', 'svuota', 'azzera', 'pulisci']) \
            or any(kw in onclick_lower for kw in ['clear', 'reset']):
        return 'Variant.Text', 'Color.Secondary'

    # Delete / Remove
    if any(kw in text_lower for kw in ['delete', 'remove', 'elimina', 'rimuovi']):
        return 'Variant.Filled', 'Color.Error'

    # Back / Previous
    if any(kw in text_lower for kw in ['indietro', 'back', 'previous', 'prev']) \
            or 'arrowback' in start_icon.lower():
        return 'Variant.Text', 'Color.Default'

    # Next / Forward
    if any(kw in text_lower for kw in ['avanti', 'next', 'forward']) \
            or 'arrowforward' in start_icon.lower():
        return 'Variant.Outlined', 'Color.Primary'

    # Refresh / Reload
    if any(kw in text_lower for kw in ['refresh', 'ricarica', 'reload']):
        return 'Variant.Outlined', 'Color.Primary'

    # Default
    return 'Variant.Outlined', 'Color.Primary'


def process_file(filepath: Path) -> int:
    content = filepath.read_text(encoding='utf-8')
    original = content

    tags = find_mud_tags(content, 'MudButton')
    changes = 0

    for start, end in reversed(tags):
        tag_text = content[start:end]

        # Skip if already has Variant
        if has_attr(tag_text, 'Variant'):
            continue

        inner_text = get_button_inner_text(content, end)
        variant, color = classify_button(tag_text, inner_text)

        # Insert Variant and Color before closing > or />
        if tag_text.endswith('/>'):
            new_tag = tag_text[:-2] + f'\n                               Variant="{variant}" Color="{color}" />'
        else:
            new_tag = tag_text[:-1] + f'\n                               Variant="{variant}" Color="{color}">'

        content = content[:start] + new_tag + content[end:]
        changes += 1

    if content != original:
        filepath.write_text(content, encoding='utf-8')

    return changes


def main():
    total = 0
    for fpath in sorted(CLIENT_DIR.rglob("*.razor")):
        n = process_file(fpath)
        if n > 0:
            print(f"  {fpath.relative_to(CLIENT_DIR)}: {n} button(s) updated")
            total += n
    print(f"\nTotal MudButton fixes: {total}")


if __name__ == '__main__':
    main()
