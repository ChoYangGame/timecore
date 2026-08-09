@echo off
if exist dist\.vercel move dist\.vercel .vercel-backup
rmdir /S /Q dist 2>nul
xcopy /E /I /Y Build dist
if exist .vercel-backup move .vercel-backup dist\.vercel
copy /Y deploy\vercel.json dist\vercel.json
rem 랭킹 API. Vercel은 api/ 폴더를 설정 없이 서버리스 함수로 잡는다.
xcopy /E /I /Y deploy\api dist\api
cd dist
vercel --prod
