
USE CommerceLabsDW;

-- Creating Report Config Table 
CREATE TABLE stage.rpt_ConfigTable(ID int IDENTITY PRIMARY KEY, SellerCentral_LoginID varbinary(MAX), SellerCentral_LoginPWD varbinary(MAX), ADS_RefreshToken varbinary(MAX),
                                   ADS_ClientID varbinary(MAX), ADS_ClientSecret varbinary(MAX), ADS_Scope varbinary(MAX), 
								   WP_Authorization varbinary(MAX), MWS_AccessID varbinary(MAX), MWS_SecretKey varbinary(MAX), MWS_MerchantID varbinary(MAX), 
								   MWS_AuthToken varbinary(MAX), Azure_ClientID varbinary(MAX), Azure_ClientSecret varbinary(MAX), Azure_TenantID varbinary(MAX), 
								   Azure_DataLakeName varbinary(MAX));

SELECT * FROM stage.rpt_ConfigTable;

SELECT * FROM sys.symmetric_keys;

-- Creating Master Key for Encryption
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'CommerceL@b$';

SELECT * FROM sys.certificates;

-- Creating Certificate
CREATE CERTIFICATE Certificate1 WITH SUBJECT = 'Protect CommerceLabs Data';

-- Creating Symmetric Key
CREATE SYMMETRIC KEY SymmetricKey1 WITH ALGORITHM = AES_256 ENCRYPTION BY CERTIFICATE Certificate1;

-- Inserting encrypted values into Report Config Table 
USE CommerceLabsDW;
OPEN SYMMETRIC KEY SymmetricKey1
DECRYPTION BY CERTIFICATE Certificate1;

INSERT INTO stage.rpt_ConfigTable(SellerCentral_LoginID, SellerCentral_LoginPWD, ADS_RefreshToken, ADS_ClientID, ADS_ClientSecret, ADS_Scope, WP_Authorization,
                                  MWS_AccessID, MWS_SecretKey, MWS_MerchantID, MWS_AuthToken, Azure_ClientID, Azure_ClientSecret, Azure_TenantID, Azure_DataLakeName) 
			VALUES(ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'geethanjali@jumpstartninja.com')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'C@d3U$^#7')), 
			       ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'Atzr|IwEBIObNzzMOX3Gq_sQ5SPTDtOPJim9G0Xm4hadZPi-YCnELc2Yk3sgP_exRasNqAXUdyG3-gioAZXvQ9lxUupPfMrnyt-6ZG5wudtapN2uQiZg3rKc5f54SQN7S-wCypUX6cOPWLkNqmJjIhQHjfR6S_4XjnZjp6k0fP7wNzGdDOLTIC7FuMZiv296MChlou21o9093Iuip9IREfNfOmQ1CXNhag9r6i6xdAM3TtSuPRKAmnq-oUEE4M9FInZ4dtP_-SK-j8dUS0z8fAF5qMqYuk87b4GGyE9YXxFf5Oi7plqlEPB9P1Behbfis0zp_3V9lkauFKFfnQNGi-LRLh7Lz-2dTiLUJovYkhHBYIFhZ9X7k82aBzW_tmdcxkjBnCqjqUsQ8-JGoHXePC6qbX_Y5DS-r2Hdfi9ecNkL4GayVAbyt5QwUfNHwTHRsV37ifoOmy6AjvICk4gfE07Sm1s-8txyWnHGC6sEC28wxwona6TLF1mDYJJh0z8O4IUzd7oZIX9poEv-XMrfxtpYdAMGhoSr3qSE1l8a-4ic80wTI4aGitafdSr1uB3KjqHwUcslKPegIUE3cjKPXp1fH251PhLjq')),
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'amzn1.application-oa2-client.79700b2ae79b4ae48f45afdec2003770')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), '51f70490ddb246385606d613a44794616de5d6f60e0112b3ba2b9f5310e344ff')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), '2905469076170529')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'MGI5YmRjOTYtZjIzZS00MzgyLWEzZjktZjU3ODg1YjVhNTAyOkRuMkp2NFg5Y3ZkanRhMDB2VzI3TjBCV05GZHRKdkVI')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'AKIAI2NAVBW5PZCAUZLA')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'xNuEXx0FWMO96BMkADfUdICtmjb98jqSBbyUT0+O')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'A2VNR33KX0E0D4')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'amzn.mws.00b5081a-f693-4fbb-a5ee-a1b9c9fc1ad6')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'ff2f4f7e-a1f5-4bc7-9cae-8af3de8e08ee')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'D9ki=lOFuTn3pp1Ozvr@Zs.-7S-=3JBi')), 
				   ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'ab49be43-adc1-4118-abf7-60a7ab7b7efb')), ENCRYPTBYKEY(KEY_GUID('SymmetricKey1'), CONVERT(varchar(max), 'cldlreportdata')));

-- Selecting the Config Values by Decrypting 
USE CommerceLabsDW;
OPEN SYMMETRIC KEY SymmetricKey1
DECRYPTION BY CERTIFICATE Certificate1;

SELECT CONVERT(varchar(max), DECRYPTBYKEY(SellerCentral_LoginID)), CONVERT(varchar(max), DECRYPTBYKEY(SellerCentral_LoginPWD)), CONVERT(varchar(max), DECRYPTBYKEY(ADS_RefreshToken)),
       CONVERT(varchar(max), DECRYPTBYKEY(ADS_ClientID)), CONVERT(varchar(max), DECRYPTBYKEY(ADS_ClientSecret)), CONVERT(varchar(max), DECRYPTBYKEY(ADS_Scope)), 
	   CONVERT(varchar(max), DECRYPTBYKEY(WP_Authorization)), CONVERT(varchar(max), DECRYPTBYKEY(MWS_AccessID)), CONVERT(varchar(max), DECRYPTBYKEY(MWS_SecretKey)), 
	   CONVERT(varchar(max), DECRYPTBYKEY(MWS_MerchantID)), CONVERT(varchar(max), DECRYPTBYKEY(MWS_AuthToken)), CONVERT(varchar(max), DECRYPTBYKEY(Azure_ClientID)), 
	   CONVERT(varchar(max), DECRYPTBYKEY(Azure_ClientSecret)), CONVERT(varchar(max), DECRYPTBYKEY(Azure_TenantID)), CONVERT(varchar(max), DECRYPTBYKEY(Azure_DataLakeName))
FROM stage.rpt_ConfigTable;


-- DROP MASTER KEY;

-- DROP TABLE stage.rpt_ConfigTable;

