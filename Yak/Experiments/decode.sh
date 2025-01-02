path=$1

sketch1="$path/RES/0"
sketch2="$path/RES/1"
Yak "execute-file-args" "decode_dan_pipeline" $sketch1 $sketch2 $2 $3 $4
