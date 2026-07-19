import os, re

ui_dir = r'Assets\_Project\Scripts\UI'
files = [f for f in os.listdir(ui_dir) if f.endswith('.cs') and f != 'UiFactory.cs']

for f in files:
    path = os.path.join(ui_dir, f)
    with open(path, 'r', encoding='utf-8') as file:
        content = file.read()
    
    # 1. Title texts
    content = re.sub(r'(UiFactory\.Label\([^,]+,\s*\"[^\"]*(Title|Header|CmdHeader|PowHeader)\"\s*,\s*[^,]+,\s*\d+,\s*[^,]+,\s*)new Color\([^)]+\)', r'\1UiFactory.Theme.TextTitle', content)
    
    # 2. Overlay panels (alpha < 0.5)
    content = re.sub(r'(UiFactory\.Panel\([^,]+,\s*\"[^\"]+\",\s*[^,]+,\s*[^,]+,\s*)new Color\([^,]+,\s*[^,]+,\s*[^,]+,\s*0\.[1-4]f?\)', r'\1UiFactory.Theme.OverlayDark', content)
    
    # 3. Main panels
    content = re.sub(r'(UiFactory\.Panel\([^,]+,\s*\"Root\",\s*[^,]+,\s*[^,]+,\s*)new Color\([^)]+\)', r'\1UiFactory.Theme.PanelBackground', content)

    # 4. Other overlay panels by name
    content = re.sub(r'(UiFactory\.Panel\([^,]+,\s*\"(List|Choices|Options|Actions)\",\s*[^,]+,\s*[^,]+,\s*)new Color\([^)]+\)', r'\1UiFactory.Theme.OverlayDark', content)
    
    # 5. Label body
    content = re.sub(r'(UiFactory\.Label\([^,]+,\s*\"[^\"]*\",\s*[^,]+,\s*([2-9][4-9]|[3-9]\d+),\s*[^,]+,\s*)new Color\([^)]+\)', r'\1UiFactory.Theme.TextBody', content)
    
    # 6. Label dim text
    content = re.sub(r'(UiFactory\.Label\([^,]+,\s*\"[^\"]*\",\s*[^,]+,\s*\d+,\s*[^,]+,\s*)new Color\([^)]+\)', r'\1UiFactory.Theme.TextDim', content)
    
    with open(path, 'w', encoding='utf-8') as file:
        file.write(content)

print('Done')
