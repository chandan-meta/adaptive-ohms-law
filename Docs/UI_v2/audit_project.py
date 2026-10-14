"""Read serialized UI, including inactive objects, and runtime construction paths."""
import csv, json, re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
out = Path(__file__).parent
guid_to_script = {}
for meta in (root / 'Assets').rglob('*.cs.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(errors='replace'), re.M)
    if match:
        guid_to_script[match[1]] = str(meta.relative_to(root)).removesuffix('.meta')
assets = []
for path in list((root/'Assets').rglob('*.unity')) + list((root/'Assets').rglob('*.prefab')):
    source = path.read_text(errors='replace')
    blocks = re.split(r'(?=^--- !u!)', source, flags=re.M)
    objects, components = {}, []
    for block in blocks:
        header = re.search(r'^--- !u!(\d+) &(-?\d+)', block)
        if not header:
            continue
        kind, ident = header.groups()
        if kind == '1':
            name = re.search(r'^  m_Name: (.*)', block, re.M)
            active = re.search(r'^  m_IsActive: (\d)', block, re.M)
            objects[ident] = {'name': name[1] if name else '', 'active':active[1] == '1' if active else None}
        elif kind in ('114','223','224'):
            owner = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', block)
            script = re.search(r'm_Script: \{fileID: \d+, guid: (\w+)', block)
            text = re.search(r'^  m_Text: (.*)', block, re.M)
            components.append({'kind':kind,'owner':owner[1] if owner else None,
                'script':guid_to_script.get(script[1],script[1]) if script else '',
                'text':text[1] if text else None})
    owners = {c['owner'] for c in components if c['kind'] in ('223','224') or '/UI/' in c['script']}
    assets.append({'asset':str(path.relative_to(root)), 'canvases':sum(c['kind']=='223' for c in components),
        'uiObjects':[dict(id=i,**obj) for i,obj in objects.items() if i in owners],
        'scripts':sorted(set(c['script'] for c in components if c['script'])),
        'serializedText':[c['text'] for c in components if c['text']]})
runtime = []
for path in (root/'Assets').rglob('*.cs'):
    source = path.read_text(errors='replace')
    if re.search(r'new GameObject|AddComponent<(?:Canvas|Text|Button|Image)|UI\.Rect|new .*UI',source):
        runtime.append({'script':str(path.relative_to(root)),
            'construction':[(i,line.strip()) for i,line in enumerate(source.splitlines(),1)
                if re.search(r'new GameObject|AddComponent<(?:Canvas|Text|Button|Image)|UI\.Rect',line)],
            'callbacks':[(i,line.strip()) for i,line in enumerate(source.splitlines(),1)
                if re.search(r'AddListener|Selected \+=|Requested \+=|VisibilityChanged \+=',line)]})
(out/'serialized-ui-audit.json').write_text(json.dumps({'assets':assets,'runtime':runtime},indent=2),encoding='utf-8')
rows = []
def row(scene,ui,binding,new):
    rows.append([scene,ui,binding,new,'Audited; Figma design pending approval; Unity migration not started'])
for a in assets:
    if a['asset'].endswith('.unity'):
        name=Path(a['asset']).stem
        if 'Scene00_FrameworkTest' in name or 'SampleScene' in name:
            row(a['asset'],'Diagnostic/sample UI; not a learner lesson','Serialized scene components','Utility/Diagnostic (retain until purpose confirmed)')
            continue
        if 'ModulePlayer' in name:
            ui,binding,new='Launch, progress, persistent controls, captions, pause/settings, chat','ModuleFlow, HudScreens, LessonPanel, SubtitleUI, AskWattsonUI','Screen/Launch; HUD/Lesson; Modal/Pause; Panel/WattsonDialogue; Panel/Chat'
        elif '04' in name or '07_Back' in name or '08_OurLab.unity' in a['asset']:
            ui,binding,new='Historical/cinematic captions, chapter banner, world labels','SceneDirector, Timeline SubtitleTrack, WorldText, LessonPanel','Screen/History; HUD/Lesson; Label/World; Panel/WattsonDialogue'
        elif '05' in name:
            ui,binding,new='Conductor prompt, cross-section counter, voltage control, charge labels','SceneDirector.Wire/Gates, WireInterior, VoltageButtons, CounterView, WorldText','Screen/Conductor; Widget/ChargeCounter; Control/Stepper; Label/World'
        elif '06' in name:
            ui,binding,new='Prediction, unmarked choices, dialogue, progress','MCQPanel or QuestionUI, SceneFlowManager, QuestionManager','Screen/Prediction; Quiz/AnswerOption; Feedback/Guidance'
        elif '08' in name:
            ui,binding,new='Instrument identification, contextual label, next instrument, companion','LabLessonController, LabSceneView, DialoguePresenter, WattsonChatUI','Screen/Instruments; Label/World; Panel/Objective; Panel/Chat'
        elif '09' in name:
            ui,binding,new='Assembly guidance, circuit schematic, terminals, incorrect topology/hints','SceneDirector.Gates or LabLessonController, CircuitAssemblyModel, LabSceneView','Screen/Circuit; Feedback/Error; Panel/Objective; Circuit/Schematic'
        elif '10' in name:
            ui,binding,new='Voltage controls, switch, readings, recording, table, evidence question','CircuitRig, ExperimentData, SceneDirector.Experiment or CircuitExperimentController, Scene07FlowManager','Screen/Experiment; Widget/Measurement; Control/Voltage; Table/Observations; Quiz/AnswerOption'
        elif '11' in name:
            ui,binding,new='Plot, row selection, line, interpretation, hints/retry','GraphUI or GraphPlotView/GraphGridGraphic, Scene08FlowManager','Screen/Graph; Graph/VoltageCurrent; Table/Observations; Feedback/Guidance'
        elif '12' in name:
            ui,binding,new='Copper/nichrome inset, ratio, slope triangle, ohm reveal, question','SceneDirector or Scene12FlowManager, MicroscopicWireView, RatioCalculationView, ResistanceGraphView','Screen/Comparison; Screen/Ratio; Screen/Resistance; Graph/VoltageCurrent; Quiz/AnswerOption'
        else:
            ui,binding,new='Equation assembly, circuit challenge, constant-temperature condition, completion/replay','SceneDirector or Scene13FlowManager, EquationAssemblyView, BenchCircuitChallenge','Screen/Equation; Screen/Challenge; Screen/Results; Feedback/Success'
        row(a['asset'],ui,binding,new)
for ui,binding,new in [
 ('Persistent controls and saved settings','LessonPanel/HudScreens/HudKeys; ModuleFlow pause reasons; VolumeSync','HUD/Lesson; Modal/Pause; Modal/Settings'),
 ('Runtime dock/cards, transient popups, loading/fade','LessonPanel.Fresh/Info/Turn/Done/End/Popup3D; HudScreens; FlowOverlay','Panel/Objective; Feedback/*; Modal/*; State/Loading'),
 ('World-space labels and formula cards','WorldText.Card; SceneBuilderBase.Callouts; LabSceneView','Label/World; Formula/Law'),
 ('Chat/voice input/error/history','AskWattsonUI; WattsonChatUI; WattsonChatManager; HoldToTalk','Panel/Chat; State/Voice; Feedback/Error'),
 ('QuestionPanel prefab incl hidden feedback/retry','Assets/AdaptiveLearning/Prefabs/UI/QuestionPanel.prefab; QuestionUI','Quiz/Question; Quiz/AnswerOption; Feedback/*'),
 ('Persistent ModuleRoot prefab incl caption/control/fade bindings','Assets/OhmsLaw/Shared/ModuleRoot.prefab; SubtitleUI; ModuleFlow','HUD/Lesson; Panel/WattsonDialogue; State/Loading')]:
    row('Shared / all applicable scenes',ui,binding,new)
with (out/'migration-inventory.csv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.writer(f);writer.writerow(['Scene or lesson step','Existing UI','Controller or binding','New Figma component','Migration status']);writer.writerows(rows)
print(json.dumps({'serializedAssets':len(assets),'sceneInventoryRows':len(rows),'runtimeConstructionScripts':len(runtime),
    'inactiveUiObjects':sum(not o['active'] for a in assets for o in a['uiObjects']), 'output':str(out)}))
