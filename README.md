# OPC UA console client

#reference : https://github.com/OPCFoundation/UA-.NETStandard


to avoide the certificate distribution add "%CommonApplicationData%\" in Secuirit configuration for storing the certificate from where Client cann consume
like :-        #<StorePath>OPC Foundation\CertificateStores\MachineDefault</StorePath>   must convert into 
                                    #<StorePath>%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault</StorePath>
