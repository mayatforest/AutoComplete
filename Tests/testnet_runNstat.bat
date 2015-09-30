del testnet_runnstart.txt
 
FOR /L %%A IN (1,1,10) DO (
testnet.bat %%A 2>>testnet_runnstart.txt
)

