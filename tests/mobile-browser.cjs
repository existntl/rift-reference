// Runs against the app's local helper, never the public marketing website.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const {spawn}=require('node:child_process');
const fs=require('node:fs');const path=require('node:path');const readline=require('node:readline');
const app=path.resolve(__dirname,'../build/app');
const token='ab'.repeat(32);let helper,browser;
(async()=>{
 helper=spawn(path.join(app,'runtime/python.exe'),['-I','-X','utf8','-u',path.join(app,'mobile_server.py')],{windowsHide:true,stdio:['pipe','pipe','pipe']});
 const ready=new Promise((resolve,reject)=>{const timer=setTimeout(()=>reject(Error('Startup timeout')),7000);readline.createInterface({input:helper.stdout}).once('line',line=>{clearTimeout(timer);resolve(JSON.parse(line))})});
 helper.stdin.write(JSON.stringify({address:'127.0.0.1',token})+'\n');
 const {url}=await ready;const fixture=JSON.parse(fs.readFileSync(path.join(app,'mobile-demo.json'),'utf8'));
 helper.stdin.write(JSON.stringify(fixture)+'\n');
 browser=await chromium.launch({headless:true,channel:'msedge'});
 const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(url);await page.locator('#content').waitFor({state:'visible'});
 await page.waitForFunction(()=>{const logo=document.querySelector('h1 img');return logo&&logo.complete&&logo.naturalWidth>0;});
 if(await page.locator('.card').count()!==10)throw Error('Roster missing');
 if((await page.locator('#content').innerText()).includes('Â'))throw Error('Unicode corrupted in transport');
 if(new URL(page.url()).hash)throw Error('Pairing token left in address bar');
 for(const [label,width,height] of [['phone',390,844],['tablet',1024,768]]){
  await page.setViewportSize({width,height});
  if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error(label+' horizontal overflow');
  await page.screenshot({path:path.join(app,'mobile-'+label+'.png')});
  await page.locator('.card').first().scrollIntoViewIfNeeded();
  await page.screenshot({path:path.join(app,'mobile-'+label+'-references.png')});
  await page.evaluate(()=>scrollTo(0,0));
 }
 const draftFixture=JSON.parse(fs.readFileSync(path.join(app,'mobile-draft.json'),'utf8'));
 helper.stdin.write(JSON.stringify(draftFixture)+'\n');
 await page.waitForFunction(()=>document.querySelectorAll('.spell').length===0);
 if((await page.locator('.notice').innerText()).includes('Cooldown'))throw Error('Draft still shows cooldown notice');
 if(!(await page.locator('#plan').innerText()).includes('VULNERABILITIES'))throw Error('Draft brief missing');
 await page.screenshot({path:path.join(app,'mobile-draft.png')});
 helper.stdin.write(JSON.stringify(fixture)+'\n');await page.locator('.spell').first().waitFor();
 const resultFixture=JSON.parse(fs.readFileSync(path.join(app,'mobile-postgame.json'),'utf8'));
 helper.stdin.write(JSON.stringify(resultFixture)+'\n');
 await page.waitForFunction(()=>document.querySelector('#focusTitle').textContent==='MATCH SUMMARY');
 if(await page.locator('.duration').count())throw Error('Postgame shows cooldown durations');
 if(!(await page.locator('#focus').innerText()).includes('32,410'))throw Error('Postgame damage missing');
 for(const [label,width,height] of [['phone',390,844],['tablet',1024,768]]){await page.setViewportSize({width,height});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth))throw Error('Postgame overflow');await page.screenshot({path:path.join(app,'postgame-'+label+'.png')});}
 helper.stdin.write(JSON.stringify(fixture)+'\n');await page.locator('.duration').first().waitFor();
 const second=await browser.newPage();await second.goto(url.split('#')[0]);await second.locator('#pair').waitFor({state:'visible'});
 if(await second.locator('#content').isVisible())throw Error('Unpaired data visible');
 await second.locator('#key').fill(token);await second.locator('#connect').click();await second.locator('#content').waitFor({state:'visible'});
 fixture.sides[0].players[0].name='<img src=x onerror=alert(1)>';
 helper.stdin.write(JSON.stringify(fixture)+'\n');
 await page.getByRole('heading',{name:'<img src=x onerror=alert(1)>'}).waitFor();
 if(await page.locator('.card-heading img').count())throw Error('Unescaped markup');
 helper.stdin.write(JSON.stringify({...fixture,phase:'Waiting',sides:[]})+'\n');
 await page.waitForFunction(()=>document.querySelectorAll('.card').length===0);
 await page.locator('#content').waitFor({state:'hidden',timeout:20000});
 if(!(await page.locator('#status').innerText()).includes('paused'))throw Error('Stale data not detected');
 helper.stdin.write(JSON.stringify(fixture)+'\n');await page.locator('#content').waitFor({state:'visible'});
 helper.stdin.end();await page.locator('#content').waitFor({state:'hidden',timeout:15000});
 if(errors.length)throw Error(errors.join('\n'));
 console.log('PASS: phone/tablet layout, QR fragment pairing, manual pairing, unpaired access, safe rendering, roster clearing and disconnect state.');
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{if(browser)await browser.close();if(helper&&!helper.killed)helper.kill()});
