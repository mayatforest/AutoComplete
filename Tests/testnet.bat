AutoCompleteClient.exe localhost 5000 <test_check_get.in >test_try%1.out
fc /L /LB5 /N test_try%1.out test_ref.out