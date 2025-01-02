for j in $(seq 0 5);do
for i in $(seq 0 25); do
    bash decode.sh $1 "${1}/RESULT-k-${i}" $i $j> temp.txt;
    python3 compare.py "${1}/RESULT-k-${i}" $2
done
done
