"""Certificate-verified HTTPS release downloader, separate from the local game pipe."""
import base64,json,pathlib,ssl,sys,urllib.request,urllib.parse
def validate(url):
    parsed=urllib.parse.urlsplit(url)
    if parsed.scheme!='https' or not parsed.hostname or parsed.username or parsed.password:
        raise ValueError('Release downloads require HTTPS')
class SafeRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        validate(newurl)
        return super().redirect_request(req,fp,code,msg,headers,newurl)
def main():
    url=base64.b64decode(sys.argv[1]).decode('utf-8');validate(url)
    destination=pathlib.Path(sys.argv[2]);limit=int(sys.argv[3])
    opener=urllib.request.build_opener(SafeRedirect(),urllib.request.HTTPSHandler(context=ssl.create_default_context()))
    with opener.open(urllib.request.Request(url,headers={'User-Agent':'RiftReference/0.4','Cache-Control':'no-cache'}),timeout=20) as response,destination.open('wb') as output:
        size=0
        while block:=response.read(65536):
            size+=len(block)
            if size>limit: raise ValueError('Download exceeds its allowed size')
            output.write(block)
    print(json.dumps({'ok':True,'bytes':size}))
if __name__=='__main__':
    try: main()
    except Exception as error:
        print(json.dumps({'ok':False,'error':type(error).__name__}));sys.exit(1)
