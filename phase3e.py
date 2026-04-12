#!/usr/bin/env python3
"""Phase 3e: Replace static Style= on MudIcon, MudChip, MudAvatar with CSS utility classes."""

import os
import re
from collections import OrderedDict

RAZOR_DIR = '/home/runner/work/EventForge/EventForge/EventForge.Client'


def parse_style(style_str):
    """Parse CSS style string into OrderedDict of {norm_key: {orig_k, orig_v, norm_v}}."""
    props = OrderedDict()
    for prop in style_str.split(';'):
        prop = prop.strip()
        if not prop or ':' not in prop:
            continue
        colon = prop.index(':')
        ok = prop[:colon].strip()
        ov = prop[colon + 1:].strip()
        nk = ok.lower()
        nv = ov.lower().replace(' ', '')
        # Normalize 0.X -> .X for opacity/decimal values
        nv = re.sub(r'\b0\.', '.', nv)
        props[nk] = {'orig_k': ok, 'orig_v': ov, 'norm_v': nv}
    return props


def get_nv(props, key):
    return props.get(key, {}).get('norm_v')


def process_mudicon(props, tag):
    """Returns (classes_to_add, keys_to_remove, add_color_inherit)."""
    classes = []
    remove = set()
    add_color = False

    fs = get_nv(props, 'font-size')
    op = get_nv(props, 'opacity')
    va = get_nv(props, 'vertical-align')
    fsh = get_nv(props, 'flex-shrink')
    col = get_nv(props, 'color')
    mr = get_nv(props, 'margin-right')
    mb = get_nv(props, 'margin-bottom')

    # Skip complex patterns with margin-bottom (rem sizes + opacity + mb)
    if mb is not None:
        return [], set(), False

    # Font-size → icon-NNpx class (only standard px sizes)
    fs_map = {
        '48px': 'icon-48', '64px': 'icon-64', '32px': 'icon-32',
        '22px': 'icon-22', '20px': 'icon-20', '16px': 'icon-16',
        '14px': 'icon-14', '12px': 'icon-12',
    }
    if fs and fs in fs_map:
        classes.append(fs_map[fs])
        remove.add('font-size')
    # else: rem sizes or unrecognised sizes left as-is

    # Opacity
    if op in ('.3', '0.3'):
        classes.append('opacity-30')
        remove.add('opacity')
    elif op in ('.2', '0.2'):
        classes.append('opacity-20')
        remove.add('opacity')

    # vertical-align: middle
    if va == 'middle':
        classes.append('vertical-middle')
        remove.add('vertical-align')

    # flex-shrink: 0
    if fsh == '0':
        classes.append('flex-no-shrink')
        remove.add('flex-shrink')

    # margin-right: 3px → mr-1 (MudBlazor spacing, ~4px)
    if mr == '3px':
        classes.append('mr-1')
        remove.add('margin-right')

    # color: white → Color="Color.Inherit" (only when font-size was already handled)
    if col == 'white' and 'font-size' in remove:
        add_color = True
        remove.add('color')

    return classes, remove, add_color


def process_mudchip(props):
    """Returns (classes_to_add, keys_to_remove)."""
    classes = []
    remove = set()

    h = get_nv(props, 'height')
    fs = get_nv(props, 'font-size')
    p = get_nv(props, 'padding')
    bg = get_nv(props, 'background')
    col = get_nv(props, 'color')
    fw = get_nv(props, 'font-weight')
    cur = get_nv(props, 'cursor')
    mh = get_nv(props, 'min-height')

    # background rgba(255,255,255,0.2) + color white → chip-white-alpha
    if bg == 'rgba(255,255,255,.2)' and col == 'white':
        classes.append('chip-white-alpha')
        remove.update(['background', 'color'])
    # background rgba(255,255,255,0.3) + color white → chip-white-alpha-3
    elif bg == 'rgba(255,255,255,.3)' and col == 'white':
        classes.append('chip-white-alpha-3')
        remove.update(['background', 'color'])

    # background #dcf8c6 + color #1a7a4a → chip-whatsapp
    if bg == '#dcf8c6' and col == '#1a7a4a' and 'background' not in remove:
        classes.append('chip-whatsapp')
        remove.update(['background', 'color'])

    # Height + font-size combos (most specific first)
    if h == '20px' and fs in ('.65rem', '0.65rem') and p == '06px' and 'height' not in remove:
        # height:20px; padding:0 6px; font-size:0.65rem → chip-xxs
        classes.append('chip-xxs')
        remove.update(['height', 'font-size', 'padding'])
    elif h == '20px' and fs in ('.7rem', '0.7rem') and 'height' not in remove:
        # height:20px; font-size:0.7rem → chip-sm (with optional padding:0 6px)
        classes.append('chip-sm')
        remove.update(['height', 'font-size'])
        if p == '06px':
            remove.add('padding')
    elif h == '18px' and fs in ('.65rem', '0.65rem') and 'height' not in remove:
        if p == '04px':
            classes.append('chip-xs')
        else:
            classes.append('chip-xxs')
        remove.update(['height', 'font-size'])
        if p in ('04px', '06px'):
            remove.add('padding')

    # font-size: 0.7rem alone (no height) → text-xs
    if fs in ('.7rem', '0.7rem') and 'font-size' not in remove and not h:
        classes.append('text-xs')
        remove.add('font-size')

    # cursor: pointer; min-height: 24px → chip-touch
    if cur == 'pointer' and mh == '24px':
        classes.append('chip-touch')
        remove.update(['cursor', 'min-height'])

    # font-weight: 600 → fw-semibold
    if fw == '600':
        classes.append('fw-semibold')
        remove.add('font-weight')

    return classes, remove


def process_mudavatar(props):
    """Returns (classes_to_add, keys_to_remove)."""
    classes = []
    remove = set()

    w = get_nv(props, 'width')
    h = get_nv(props, 'height')
    fs = get_nv(props, 'font-size')
    bg = get_nv(props, 'background')
    col = get_nv(props, 'color')
    fsh = get_nv(props, 'flex-shrink')
    fw = get_nv(props, 'font-weight')

    size_map = {
        ('88px', '88px'): 'avatar-88',
        ('64px', '64px'): 'avatar-64',
        ('44px', '44px'): 'avatar-44',
        ('40px', '40px'): 'avatar-40',
        ('36px', '36px'): 'avatar-36',
    }

    if w and h and (w, h) in size_map:
        classes.append(size_map[(w, h)])
        remove.update(['width', 'height'])
        if fs:
            remove.add('font-size')  # absorbed into size class

    # background/color patterns
    if bg == 'var(--mud-palette-primary)' and col in ('#fff', '#ffffff', 'white'):
        classes.append('avatar-primary')
        remove.update(['background', 'color'])
    elif bg == '#25d366' and 'background' not in remove:
        classes.append('avatar-whatsapp')
        remove.add('background')
        if col == 'white':
            remove.add('color')

    # flex-shrink: 0
    if fsh == '0':
        classes.append('flex-no-shrink')
        remove.add('flex-shrink')

    # font-weight: 600
    if fw == '600':
        classes.append('fw-semibold')
        remove.add('font-weight')

    return classes, remove


def extract_tag_end(content, start):
    """Return the index just after the closing > of the opening tag starting at 'start'."""
    j = start
    in_str = False
    depth = 0  # @() expression depth
    while j < len(content):
        ch = content[j]
        if in_str:
            if ch == '"' and depth == 0:
                in_str = False
        elif ch == '"':
            in_str = True
        elif ch == '@' and j + 1 < len(content) and content[j + 1] == '(':
            depth += 1
            j += 2
            continue
        elif depth > 0:
            if ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
        elif ch == '>':
            return j + 1
        j += 1
    return len(content)


def update_style_in_tag(tag, new_style):
    """Replace or remove the Style attribute in a tag."""
    if new_style:
        return re.sub(r'\bStyle="[^"]*"', f'Style="{new_style}"', tag)
    else:
        # Remove Style attribute and any whitespace that preceded it on the same line
        result = re.sub(r'[ \t]+Style="[^"]*"', '', tag)
        # Fallback: if Style was at start (no leading whitespace on same line)
        result = re.sub(r'Style="[^"]*"[ \t]*', '', result)
        return result


def add_class_to_tag(tag, new_classes):
    """Append classes to existing Class attribute, or add a new one before the tag close."""
    class_str = ' '.join(new_classes)
    class_m = re.search(r'\bClass="([^"]*)"', tag)
    if class_m:
        existing = class_m.group(1).strip()
        new_val = (existing + ' ' + class_str).strip()
        return tag[:class_m.start()] + f'Class="{new_val}"' + tag[class_m.end():]
    else:
        # Insert before the closing /> or >
        m = re.search(r'(\s*)(/>|>)$', tag)
        if m:
            return tag[:m.start()] + f' Class="{class_str}"' + m.group(1) + m.group(2)
        return tag + f' Class="{class_str}"'


def add_attr_to_tag(tag, attr_name, attr_val):
    """Add an attribute before the tag close."""
    m = re.search(r'(\s*)(/>|>)$', tag)
    if m:
        return tag[:m.start()] + f' {attr_name}="{attr_val}"' + m.group(1) + m.group(2)
    return tag + f' {attr_name}="{attr_val}"'


def apply_tag_changes(comp, tag):
    """Apply Style→Class transformations to a component tag. Returns modified tag."""
    style_m = re.search(r'\bStyle="([^"]*)"', tag)
    if not style_m:
        return tag

    style_val = style_m.group(1).strip()
    if '@' in style_val:  # dynamic — skip
        return tag

    props = parse_style(style_val)

    if comp == 'MudIcon':
        new_classes, remove_keys, add_color = process_mudicon(props, tag)
    elif comp == 'MudChip':
        new_classes, remove_keys = process_mudchip(props)
        add_color = False
    elif comp == 'MudAvatar':
        new_classes, remove_keys = process_mudavatar(props)
        add_color = False
    else:
        return tag

    if not new_classes and not remove_keys:
        return tag

    # Rebuild remaining style preserving original key/value formatting
    remaining = [
        (props[k]['orig_k'], props[k]['orig_v'])
        for k in props
        if k not in remove_keys
    ]
    new_style = ';'.join(f'{k}:{v}' for k, v in remaining) if remaining else None

    new_tag = tag
    new_tag = update_style_in_tag(new_tag, new_style)
    if new_classes:
        new_tag = add_class_to_tag(new_tag, new_classes)
    if add_color:
        if not re.search(r'\bColor="', new_tag):
            new_tag = add_attr_to_tag(new_tag, 'Color', 'Color.Inherit')

    return new_tag


def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    for comp in ('MudIcon', 'MudChip', 'MudAvatar'):
        pattern = re.compile(r'<' + re.escape(comp) + r'(?=[\s>/])')
        parts = []
        last_end = 0

        for m in pattern.finditer(content):
            start = m.start()
            end = extract_tag_end(content, start)
            tag = content[start:end]
            new_tag = apply_tag_changes(comp, tag)
            parts.append(content[last_end:start])
            parts.append(new_tag)
            last_end = end

        parts.append(content[last_end:])
        content = ''.join(parts)

    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False


def main():
    changed = []
    for root, dirs, files in os.walk(RAZOR_DIR):
        dirs[:] = [d for d in dirs if d not in ('bin', 'obj')]
        for fn in files:
            if fn.endswith('.razor'):
                fp = os.path.join(root, fn)
                if process_file(fp):
                    changed.append(fp)

    print(f"Modified {len(changed)} files:")
    for f in sorted(changed):
        print(f"  {os.path.relpath(f, RAZOR_DIR)}")


if __name__ == '__main__':
    main()
