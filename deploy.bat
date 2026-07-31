@echo off
if exist dist\.vercel move dist\.vercel .vercel-backup
rmdir /S /Q dist 2>nul
xcopy /E /I /Y Build dist
if exist .vercel-backup move .vercel-backup dist\.vercel
copy /Y deploy\vercel.json dist\vercel.json
cd dist
vercel --prod
