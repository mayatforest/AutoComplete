del test_console.txt
del test_console_all.txt

FOR /L %%A IN (1,1,10) DO (
test_console.bat %%A 2>>test_console.txt 

)







