#!/usr/bin/env sh

# abort on errors
set -e

npm install -g vuepress
# build
npm run docs:build

# navigate into the build output directory
cd ./gh-pages

# if you are deploying to a custom domain
# echo 'www.example.com' > CNAME

git init && \
git config --global user.email @users.noreply.github.com && \
git config --global user.name "Git Hub Deploy Action" && \
git add . 
git commit -m 'deploy'

git push -f git@github.com:${GH_REPOSITORY}.git master:gh-pages

