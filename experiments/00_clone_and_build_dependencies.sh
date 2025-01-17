#!/bin/bash

directories="LittleSharp FlashHash SymmetricDifferenceFinder KMerUtils RedaFasta";

echo $directories 

cd ../..

for i in $directories; do 
    if [ ! -d $i ] ; then
	git clone "https://github.com/vojtechgadurek/"$i".git"
    fi
done

for i in $directories; do
    dotnet build $i
    echo $i "Finished"
done

dotnet build Yak
