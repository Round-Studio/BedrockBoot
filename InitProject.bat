@echo off
echo 正在初始化仓库...

git submodule
git pull
git submodule update --init --recursive --remote

echo 仓库初始化完毕。