@echo off
copy /Y deploy\vercel.json Build\vercel.json
cd Build
vercel --prod
