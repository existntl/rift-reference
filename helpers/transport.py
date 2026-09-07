"""Private pipe transport; only explicit loadout creation can write to League."""
import json, sys, ssl, urllib.request, urllib.parse, re
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise ValueError('Redirects are disabled')
opener=urllib.request.build_opener(urllib.request.ProxyHandler({}),NoRedirect(),urllib.request.HTTPSHandler(context=ssl._create_unverified_context()))
for line in sys.stdin:
    try:
        message=json.loads(line)
        url=message['url']; parsed=urllib.parse.urlsplit(url)
        if parsed.scheme!='https' or parsed.hostname!='127.0.0.1' or parsed.username or parsed.password:
            raise ValueError('Only local League endpoints are supported')
        method=message.get('method','GET')
        loadout=parsed.path=='/lol-perks/v1/pages' or re.fullmatch(r'/lol-item-sets/v1/item-sets/[1-9][0-9]*/sets',parsed.path)
        if parsed.query or parsed.fragment or method not in ('GET','POST'):
            raise ValueError('Request not allowed')
        if method=='GET' and parsed.path!='/lol-end-of-game/v1/eog-stats-block' and not parsed.path.startswith(('/liveclientdata/','/lol-gameflow/','/lol-summoner/','/lol-champ-select/')):
            raise ValueError('Endpoint not allowed')
        if method=='POST' and (not loadout or not message.get('auth') or not isinstance(message.get('body'),dict)):
            raise ValueError('Write not allowed')
        headers={}
        if message.get('auth'): headers['Authorization']='Basic '+message['auth']
        body=None
        if method=='POST':
            body=json.dumps(message['body']).encode('utf-8')
            if len(body)>65536: raise ValueError('Loadout too large')
            headers['Content-Type']='application/json'
        with opener.open(urllib.request.Request(url,headers=headers,data=body,method=method),timeout=2) as response:
            raw=response.read()
            result={'ok':True,'data':json.loads(raw) if raw else None}
    except Exception as error:
        result={'ok':False,'error':type(error).__name__}
    print(json.dumps(result,ensure_ascii=True),flush=True)
