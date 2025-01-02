#!/bin/bash
cd /workspaces/codespaces-blank

directories="LittleSharp FlashHash SymmetricDifferenceFinder Yak  KMerUtils RedaFasta";

echo $directories 


for i in $directories; do 
    git clone "https://github.com/vojtechgadurek/"$i".git"
done

for i in $directories; do
    dotnet build $i
    echo $i "Finished"
done
