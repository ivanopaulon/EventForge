#!/usr/bin/env python3
"""
Phase 3 - Task 5: Fix MudIcon Style= → use Color= and Size= params.
"""

import re
from pathlib import Path

CLIENT_DIR = Path("/home/runner/work/EventForge/EventForge/EventForge.Client")

PALETTE_COLOR_MAP = {
    'primary': 'Color.Primary',
    'secondary': 'Color.Secondary',
    'error': 'Color.Error',
    'warning': 'Color.Warning',
    'success': 'Color.Success',
    'info': 'Color.Info',
}

FONT_SIZE_MAP = {
    '16px': 'Size.Small',
    '24px': 'Size.Medium',
    '32px': 'Size.Large',
}


def find_mud_tags(content: str, component_name: str):
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


def get_attr_value(tag_text: str, attr_name: str):
    pattern = re.compile(re.escape(attr_name) + r'\s*=\s*"([^"]*)"', re.IGNORECASE)
    m = pattern.search(tag_text)
    return m.group(1) if m else None


def has_attr(tag_text: str, attr_name: str) -> bool:
    return bool(re.search(r'\b' + re.escape(attr_name) + r'\s*=', tag_text, re.IGNORECASE))


def remove_attr(tag_text: str, attr_name: str) -> str:
    """Remove an attribute (name="value") from tag text."""
    pattern = re.compile(
        r'\s*' + re.escape(attr_name) + r'\s*=\s*"[^"]*"',
        re.IGNORECASE
    )
    return pattern.sub('', tag_text)


def insert_attr(tag_text: str, attr: str) -> str:
    """Insert attribute before the closing > or />."""
    if tag_text.endswith('/>'):
        return tag_text[:-2].rstrip() + ' ' + attr + ' />'
    elif tag_text.endswith('>'):
        return tag_text[:-1].rstrip() + ' ' + attr + '>'
    return tag_text


def process_mud_icon_tag(tag_text: str) -> str:
    style_val = get_attr_value(tag_text, 'Style')
    if not style_val:
        return tag_text

    # Normalise style value
    style_norm = re.sub(r'\s+', '', style_val).lower().rstrip(';')

    # Try to parse individual declarations
    decls = [d.strip() for d in re.split(r';', style_val) if d.strip()]

    color_decl = None
    size_decl = None
    other_decls = []

    for decl in decls:
        d_lower = re.sub(r'\s+', '', decl).lower()
        # color: var(--mud-palette-xxx)
        m = re.match(r'color:var\(--mud-palette-([a-z\-]+)\)', d_lower)
        if m:
            palette_key = m.group(1)
            if palette_key in PALETTE_COLOR_MAP:
                color_decl = PALETTE_COLOR_MAP[palette_key]
            else:
                other_decls.append(decl)
            continue
        # font-size: Npx  (check map)
        m = re.match(r'font-size:(\d+px|\d+rem)', d_lower)
        if m:
            fs = m.group(1)
            if fs in FONT_SIZE_MAP:
                size_decl = FONT_SIZE_MAP[fs]
            else:
                other_decls.append(decl)
            continue
        other_decls.append(decl)

    if color_decl is None and size_decl is None:
        return tag_text  # nothing to do

    new_tag = tag_text

    # Remove Style attr
    new_tag = remove_attr(new_tag, 'Style')

    # Re-add Style if there are remaining declarations
    if other_decls:
        remaining_style = '; '.join(other_decls) + ';'
        new_tag = insert_attr(new_tag, f'Style="{remaining_style}"')

    # Add Color= if not already present
    if color_decl and not has_attr(new_tag, 'Color'):
        new_tag = insert_attr(new_tag, f'Color="{color_decl}"')

    # Add Size= if not already present
    if size_decl and not has_attr(new_tag, 'Size'):
        new_tag = insert_attr(new_tag, f'Size="{size_decl}"')

    return new_tag


def process_file(filepath: Path) -> int:
    content = filepath.read_text(encoding='utf-8')
    original = content

    tags = find_mud_tags(content, 'MudIcon')
    changes = 0

    for start, end in reversed(tags):
        tag_text = content[start:end]
        new_tag = process_mud_icon_tag(tag_text)
        if new_tag != tag_text:
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
            print(f"  {fpath.relative_to(CLIENT_DIR)}: {n} MudIcon(s) updated")
            total += n
    print(f"\nTotal MudIcon fixes: {total}")


if __name__ == '__main__':
    main()
