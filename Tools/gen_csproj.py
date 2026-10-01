"""Generates standalone csproj files mirroring Unity's Assembly-CSharp(-Editor).csproj so `dotnet build`
can compile every script under Assets/ outside Unity. Usage: python Tools/gen_csproj.py <output folder>"""
import os
import re
import sys

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.environ.get("LOCALAPPDATA", "."), "Temp", "ohm_cc", "default")
os.makedirs(out, exist_ok=True)
BS = chr(92)
esc = lambda p: p.replace("'", "%27")


def fix(src, name, compile_items, extra=""):
    s = open(os.path.join(root, src), encoding="utf-8-sig").read()
    s = re.sub(r'\s*<Compile Include="[^"]*" />', "", s)
    s = re.sub(r'\s*<Analyzer Include="[^"]*" />', "", s)
    s = re.sub(r"<HintPath>(?![A-Za-z]:)", lambda m: "<HintPath>" + esc(root) + BS, s)
    s = re.sub(r'<ProjectReference Include="Assembly-CSharp.csproj">.*?</ProjectReference>', lambda m: extra, s, flags=re.S)
    s = re.sub(r"<OutputPath>[^<]*</OutputPath>", lambda m: "<OutputPath>" + out + BS + "bin" + BS + name + BS + "</OutputPath>", s)
    s = s.replace("<BaseDirectory>.</BaseDirectory>", "<BaseDirectory>.</BaseDirectory><BaseIntermediateOutputPath>" + out + BS + "obj" + BS + name + BS + "</BaseIntermediateOutputPath>")
    s = s.replace("</Project>", "<ItemGroup>" + compile_items + "</ItemGroup></Project>")
    open(os.path.join(out, name + ".csproj"), "w", encoding="utf-8").write(s)


files = []
for d, _, fs in os.walk(os.path.join(root, "Assets")):
    for f in fs:
        if f.endswith(".cs"):
            files.append(os.path.join(d, f))
is_editor = lambda f: (BS + "Editor" + BS) in f
rt = "".join('<Compile Include="%s" />' % esc(f) for f in files if not is_editor(f))
ed = "".join('<Compile Include="%s" />' % esc(f) for f in files if is_editor(f))
fix("Assembly-CSharp.csproj", "Runtime", rt)
fix("Assembly-CSharp-Editor.csproj", "Editor", ed,
    '<Reference Include="Assembly-CSharp"><HintPath>' + out + BS + "bin" + BS + "Runtime" + BS + "Assembly-CSharp.dll</HintPath></Reference>")
