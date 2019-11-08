# Instructions to get started. 

## Prerequisites

### YARN

Install vuepress globally using yarn. Here are the instructions for debian which can be used for WSL as well.

https://yarnpkg.com/en/docs/install#debian-stable


```sh
curl -sS https://dl.yarnpkg.com/debian/pubkey.gpg | sudo apt-key add -
echo "deb https://dl.yarnpkg.com/debian/ stable main" | sudo tee /etc/apt/sources.list.d/yarn.list

sudo apt update && sudo apt install yarn

```

> Make sure you have yarn in your path. 

## Vuepress. 

```sh
yarn global add vuepress
```


# To view the pages

```sh
cd ./pages/
vuepress dev .
```

# Publish content
https://v1.vuepress.vuejs.org/guide/deploy.html#github-pages

```sh
cd pages

vuepress build .

cd gh-pages
git init
git add -A
git commit -m 'deploy'
git push -f git@github.com:Azure/acr.git master:gh-pages

```
vuepress build