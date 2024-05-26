#!/bin/sh

git grep -E "$(cat 'grep-secrets-regex.txt')"
