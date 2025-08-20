#!/bin/bash

pathToProject=/home/tim/Source/projects/SimpleScriptWebSite/
pathToDeploy=myApps/SimpleScriptWebsite/
CURRENT_DATE=$(date +"%Y-%m-%d %H:%M:%S")

echo "Step 1: Backing up project files to git..."
cd $pathToProject || exit 1
git pull > /dev/null || exit 1
git add . > /dev/null || exit 1
git commit -m "Backup for Deploy $CURRENT_DATE" > /dev/null || exit 1
git push > /dev/null || exit 1
echo "✓ Step 1: Project files backed up successfully"

echo "Step 2: Deploying to remote server..."
ssh myserver "
    cd $pathToDeploy &&
    git pull &&
    docker compose down &&
    docker compose up --build -d
" > /dev/null || exit 1
echo "✓ Step 2: Remote deployment completed successfully"

echo "🎉 All steps completed successfully!" 

