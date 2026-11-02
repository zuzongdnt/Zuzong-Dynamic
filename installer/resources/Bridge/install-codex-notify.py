from pathlib import Path
import json,re,tomllib,shutil,datetime,argparse

parser=argparse.ArgumentParser()
parser.add_argument('--app',required=True)
parser.add_argument('--config',default=str(Path.home()/'.codex/config.toml'))
args=parser.parse_args()
app=Path(args.app).resolve();config=Path(args.config).resolve()
before=config.read_text(encoding='utf-8-sig');parsed=tomllib.loads(before)
original=parsed.get('notify',[])
if not isinstance(original,list) or not all(isinstance(x,str) for x in original):raise RuntimeError('Existing notify is not a string array; no config was changed.')
bridge=app/'Bridge/ZuzongNotify.exe';bridge_config=app/'Bridge/config.json'
if not bridge.exists():raise RuntimeError('Bridge executable missing; no config was changed.')
if original and Path(original[0]).name.lower()=='zuzongnotify.exe':
 old_config=Path(original[original.index('--config')+1])
 original=json.loads(old_config.read_text(encoding='utf-8-sig'))['forward']
replacement=[str(bridge),'--config',str(bridge_config)]
lines=before.splitlines(keepends=True);found=[];in_table=False
for i,line in enumerate(lines):
 if line.lstrip().startswith('['):in_table=True
 if not in_table and re.match(r'^\s*notify\s*=',line):found.append(i)
if len(found)>1:raise RuntimeError('Duplicate notify; no config was changed.')
newline='notify = '+json.dumps(replacement,ensure_ascii=False)+'\n'
if found:
 # shortcut: current notify is a single line; reject multiline instead of risking unrelated TOML.
 i=found[0]
 try:tomllib.loads(lines[i])
 except Exception:raise RuntimeError('Multiline notify needs a dedicated parser; no config was changed.')
 lines[i]=newline
else:lines.insert(0,newline)
after=''.join(lines);new=tomllib.loads(after)
assert {k:v for k,v in parsed.items() if k!='notify'}=={k:v for k,v in new.items() if k!='notify'}
bridge_config.parent.mkdir(parents=True,exist_ok=True)
profile=Path.home()/'AppData/Local/ZuzongDynamic'
profile.mkdir(parents=True,exist_ok=True)
bridge_config.write_text(json.dumps(dict(routeFile=str(profile/'notify-route.json'),fallbackData=str(profile),forward=original),ensure_ascii=False,indent=2),encoding='utf-8')
(profile/'notify-route.json').write_text(json.dumps(str(profile)),encoding='utf-8')
stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f')
backup=config.with_name(config.name+'.before-zuzong-notify-'+stamp+'.bak')
shutil.copy2(config,backup)
tmp=config.with_name(config.name+'.zuzong-new');tmp.write_text(after,encoding='utf-8');tmp.replace(config)
assert tomllib.loads(config.read_text(encoding='utf-8'))['notify']==replacement
print('PASS notify bridge installed; existing command preserved; all other TOML values unchanged.')
print('Backup:',backup)
