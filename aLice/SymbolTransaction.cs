using System.Globalization;
using System.Text;
using CatSdk;
using CatSdk.Symbol;
using CatSdk.Utils;

namespace aLice;

public static class SymbolTransaction
{
    public static string ParseTransaction(string hex, bool embedded = false, string pubkey = "")
    {
        var transaction = embedded ? EmbeddedTransactionFactory.Deserialize(hex) : TransactionFactory.Deserialize(hex);
        if (!embedded) {
            ((ITransaction) transaction).SignerPublicKey = new PublicKey(Converter.HexToBytes(pubkey));
        }
        if (transaction.Type == TransactionType.TRANSFER)
        {
            return ParseTransferTransaction(transaction, embedded);
        } 
        if(transaction.Type == TransactionType.MOSAIC_DEFINITION)
        {
            return ParseMosaicDefinitionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_SUPPLY_CHANGE)
        {
            return ParseMosaicSupplyChangeTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_SUPPLY_REVOCATION)
        {
            return ParseMosaicSupplyRevocationTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ACCOUNT_KEY_LINK)
        {
            return ParseAccountKeyLinkTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.NODE_KEY_LINK)
        {
            return ParseNodeKeyLinkTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.VOTING_KEY_LINK)
        {
            return ParseVotingKeyLinkTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.VRF_KEY_LINK)
        {
            return ParseVrfKeyLinkTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.HASH_LOCK)
        {
            return ParseHashLockTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.SECRET_LOCK)
        {
            return ParseSecretLockTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.SECRET_PROOF)
        {
            return ParseSecretProofTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ACCOUNT_METADATA)
        {
            return ParseAccountMetadataTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_METADATA)
        {
            return ParseMosaicMetadataTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.NAMESPACE_METADATA)
        {
            return ParseNamespaceMetadataTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MULTISIG_ACCOUNT_MODIFICATION)
        {
            return ParseMultisigAccountModificationTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ADDRESS_ALIAS)
        {
            return ParseAddressAliasTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_ALIAS)
        {
            return ParseMosaicAliasTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.NAMESPACE_REGISTRATION)
        {
            return ParseNamespaceRegistrationTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ACCOUNT_ADDRESS_RESTRICTION)
        {
            return ParseAccountAddressRestrictionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ACCOUNT_MOSAIC_RESTRICTION)
        {
            return ParseAccountMosaicRestrictionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.ACCOUNT_OPERATION_RESTRICTION)
        {
            return ParseAccountOperationRestrictionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_ADDRESS_RESTRICTION)
        {
            return ParseMosaicAddressRestrictionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.MOSAIC_GLOBAL_RESTRICTION)
        {
            return ParseMosaicGlobalRestrictionTransaction(transaction, embedded);
        }
        if(transaction.Type == TransactionType.AGGREGATE_COMPLETE)
        {
            return ParseAggregateCompleteTransaction(transaction);
        }
        if(transaction.Type == TransactionType.AGGREGATE_BONDED)
        {
            return ParseAggregateBondedTransaction(transaction);
        }
        throw new Exception("Unknown transaction type");
    }
    
    static string ParseCosignature(Cosignature cosignature)
    {
        var result = "";
        result += $"\tSignerPublicKey: {cosignature.SignerPublicKey}\n";
        result += $"\tSignature: {cosignature.Signature}\n";
        return result;
    }

    static string ParseAggregateCompleteTransaction(IBaseTransaction _transaction)
    {
        var result = "";
        var transaction = (AggregateCompleteTransactionV2) _transaction;
        result += "AggregateCompleteTransaction\n";
        result += $"TransactionsHash: {transaction.TransactionsHash}\n";
        result += $"Transactions:\n";
        result = transaction.Transactions.Aggregate(result, (current, innerTransaction) => 
            current + $"{ParseTransaction(Converter.BytesToHex(innerTransaction.Serialize()), true)}\n");
        result += $"Cosignatures:\n";
        result = transaction.Cosignatures.Aggregate(result, (current, cosignature) => 
            current + $"{ParseCosignature(cosignature)}\n");
        result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        return result;
    }

    static string ParseAggregateBondedTransaction(IBaseTransaction _transaction)
    {
        var result = "";
        var transaction = (AggregateBondedTransactionV2) _transaction;
        result += "AggregateBondedTransaction\n";
        result += $"TransactionsHash: {transaction.TransactionsHash}\n";
        result += $"Transactions:\n";
        result = transaction.Transactions.Aggregate(result, (current, innerTransaction) => 
            current + $"{ParseTransaction(Converter.BytesToHex(innerTransaction.Serialize()), true)}\n");
        result += $"Cosignatures:\n";
        result = transaction.Cosignatures.Aggregate(result, (current, cosignature) => 
            current + $"{ParseCosignature(cosignature)}\n");
        result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        return result;
    }

    static string ParseMosaicGlobalRestrictionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicGlobalRestrictionTransactionV1) _transaction;
            result += "MosaicGlobalRestrictionTransaction\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"RestrictionKey: {transaction.RestrictionKey}\n";
            result += $"PreviousRestrictionValue: {transaction.PreviousRestrictionValue}\n";
            result += $"NewRestrictionValue: {transaction.NewRestrictionValue}\n";
            result += $"PreviousRestrictionType: {transaction.PreviousRestrictionType}\n";
            result += $"NewRestrictionType: {transaction.NewRestrictionType}\n";
        }
        else
        {
            var transaction = (MosaicGlobalRestrictionTransactionV1) _transaction;
            result += "MosaicGlobalRestrictionTransaction\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"RestrictionKey: {transaction.RestrictionKey}\n";
            result += $"PreviousRestrictionValue: {transaction.PreviousRestrictionValue}\n";
            result += $"NewRestrictionValue: {transaction.NewRestrictionValue}\n";
            result += $"PreviousRestrictionType: {transaction.PreviousRestrictionType}\n";
            result += $"NewRestrictionType: {transaction.NewRestrictionType}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);   
        }
        return result;
    }

    static string ParseMosaicAddressRestrictionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicAddressRestrictionTransactionV1) _transaction;
            result += "MosaicAddressRestrictionTransaction\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"RestrictionKey: {transaction.RestrictionKey}\n";
            result += $"PreviousRestrictionValue: {transaction.PreviousRestrictionValue}\n";
            result += $"NewRestrictionValue: {transaction.NewRestrictionValue}\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
        }
        else
        {
            var transaction = (MosaicAddressRestrictionTransactionV1) _transaction;
            result += "MosaicAddressRestrictionTransaction\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"RestrictionKey: {transaction.RestrictionKey}\n";
            result += $"PreviousRestrictionValue: {transaction.PreviousRestrictionValue}\n";
            result += $"NewRestrictionValue: {transaction.NewRestrictionValue}\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);   
        }
        return result;
    }


    static string ParseAccountOperationRestrictionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAccountOperationRestrictionTransactionV1) _transaction;
            result += "AccountOperationRestrictionTransaction\n";
            result += "AccountMosaicRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, additions) => 
                current + $"\t{TransactionTypeValueToKey(additions.Value)}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, deletions) => 
                current + $"\t{TransactionTypeValueToKey(deletions.Value)}\n");
        }
        else
        {
            var transaction = (AccountOperationRestrictionTransactionV1) _transaction;
            result += "AccountOperationRestrictionTransaction\n";
            result += "AccountMosaicRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, additions) => 
                current + $"\t{TransactionTypeValueToKey(additions.Value)}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, deletions) => 
                current + $"\t{TransactionTypeValueToKey(deletions.Value)}\n");
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string TransactionTypeValueToKey(uint value) {
        var values = new uint[]{
            16716, 16972, 16705, 16961, 16707, 16963, 16712, 16722, 16978, 16708, 16964, 17220, 16717, 16973, 17229, 16725, 16974, 17230,
            16718, 16720, 16976, 17232, 16977, 16721, 16724
        };
        var keys = new []{
            "ACCOUNT_KEY_LINK", "NODE_KEY_LINK", "AGGREGATE_COMPLETE", "AGGREGATE_BONDED", "VOTING_KEY_LINK", "VRF_KEY_LINK", "HASH_LOCK",
            "SECRET_LOCK", "SECRET_PROOF", "ACCOUNT_METADATA", "MOSAIC_METADATA", "NAMESPACE_METADATA", "MOSAIC_DEFINITION",
            "MOSAIC_SUPPLY_CHANGE", "MOSAIC_SUPPLY_REVOCATION", "MULTISIG_ACCOUNT_MODIFICATION", "ADDRESS_ALIAS", "MOSAIC_ALIAS",
            "NAMESPACE_REGISTRATION", "ACCOUNT_ADDRESS_RESTRICTION", "ACCOUNT_MOSAIC_RESTRICTION", "ACCOUNT_OPERATION_RESTRICTION",
            "MOSAIC_ADDRESS_RESTRICTION", "MOSAIC_GLOBAL_RESTRICTION", "TRANSFER"
        };

        var index = Array.IndexOf(values, value);
        if (-1 == index)
            throw new Exception($"invalid enum value {value}");

        return keys[index];
    }

    static string ParseAccountMosaicRestrictionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAccountMosaicRestrictionTransactionV1) _transaction;
            result += "AccountMosaicRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, additions) => 
                current + $"\t{additions.Value:X16}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, deletions) => 
                current + $"\t{deletions.Value:X16}\n");
        }
        else
        {
            var transaction = (AccountMosaicRestrictionTransactionV1) _transaction;
            result += "AccountMosaicRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, additions) => 
                current + $"\t{additions.Value:X16}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, deletions) => 
                current + $"\t{deletions.Value:X16}\n");
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        return result;
    }

    static string ParseAccountAddressRestrictionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAccountAddressRestrictionTransactionV1) _transaction;
            result += "AccountAddressRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, transactionAddressAdditions) => 
                current + $"\t{Converter.AddressToString(transactionAddressAdditions.bytes)}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, transactionAddressDeletions) => 
                current + $"\t{Converter.AddressToString(transactionAddressDeletions.bytes)}\n");
        }
        else
        {
            var transaction = (AccountAddressRestrictionTransactionV1) _transaction;
            result += "AccountAddressRestrictionTransaction\n";
            result += $"RestrictionFlags:\n";
            result += $"{ParseRestrictionFlags(transaction.RestrictionFlags.Value)}";
            result += $"RestrictionAdditions:\n";
            result = transaction.RestrictionAdditions.Aggregate(result, (current, transactionAddressAdditions) => 
                current + $"\t{Converter.AddressToString(transactionAddressAdditions.bytes)}\n");
            result += $"RestrictionDeletions:\n";
            result = transaction.RestrictionDeletions.Aggregate(result, (current, transactionAddressDeletions) => 
                current + $"\t{Converter.AddressToString(transactionAddressDeletions.bytes)}\n");
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseRestrictionFlags(ushort flags)
    {
        var allFlags = new List<AccountRestrictionFlags>
        {
            AccountRestrictionFlags.ADDRESS,
            AccountRestrictionFlags.MOSAIC_ID,
            AccountRestrictionFlags.TRANSACTION_TYPE,
            AccountRestrictionFlags.OUTGOING,
            AccountRestrictionFlags.BLOCK
        };

        return allFlags.Where(flag => (flags & flag.Value) == flag.Value).Aggregate("", (current, flag) => current + $"\t{flag}\n");
    }

    static string ParseNamespaceRegistrationTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedNamespaceRegistrationTransactionV1) _transaction;
            result += "NamespaceRegistrationTransaction\n";
            result += $"ParentId: {transaction.ParentId.Value:X16}\n";
            result += $"Id: {transaction.Id.Value:X16}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"RegistrationType: {transaction.RegistrationType}\n";
        }
        else
        {
            var transaction = (NamespaceRegistrationTransactionV1) _transaction;
            result += "NamespaceRegistrationTransaction\n";
            result += $"ParentId: {transaction.ParentId.Value:X16}\n";
            result += $"Id: {transaction.Id.Value:X16}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"RegistrationType: {transaction.RegistrationType}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseMosaicAliasTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicAliasTransactionV1) _transaction;
            result += "MosaicAliasTransaction\n";
            result += $"NamespaceId: {transaction.NamespaceId.Value:X16}\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"AliasAction: {transaction.AliasAction}\n";
        }
        else
        {
            var transaction = (MosaicAliasTransactionV1) _transaction;
            result += "MosaicAliasTransaction\n";
            result += $"NamespaceId: {transaction.NamespaceId.Value:X16}\n";
            result += $"MosaicId: {transaction.MosaicId.Value:X16}\n";
            result += $"AliasAction: {transaction.AliasAction}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseAddressAliasTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAddressAliasTransactionV1) _transaction;
            result += "AddressAliasTransaction\n";
            result += $"NamespaceId: {transaction.NamespaceId.Value:X16}\n";
            result += $"Address: {Converter.AddressToString(transaction.Address.bytes)}\n";
            result += $"AliasAction: {transaction.AliasAction}\n";
        }
        else
        {
            var transaction = (AddressAliasTransactionV1) _transaction;
            result += "AddressAliasTransaction\n";
            result += $"NamespaceId: {transaction.NamespaceId.Value:X16}\n";
            result += $"Address: {Converter.AddressToString(transaction.Address.bytes)}\n";
            result += $"AliasAction: {transaction.AliasAction}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseMultisigAccountModificationTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMultisigAccountModificationTransactionV1) _transaction;
            result += "NamespaceMetadataTransaction\n";
            result += $"MinApprovalDelta: {transaction.MinApprovalDelta}\n";
            result += $"MinRemovalDelta: {transaction.MinRemovalDelta}\n";
            result += $"AddressAdditions:\n";
            result = transaction.AddressAdditions.Aggregate(result, (current, transactionAddressAdditions) => 
                current + $"\t{Converter.AddressToString(transactionAddressAdditions.bytes)}\n");
            result += $"AddressDeletions:\n";
            result = transaction.AddressAdditions.Aggregate(result, (current, transactionAddressDeletions) => 
                current + $"\t{Converter.AddressToString(transactionAddressDeletions.bytes)}\n");
        }
        else
        {
            var transaction = (MultisigAccountModificationTransactionV1) _transaction;
            result += "NamespaceMetadataTransaction\n";
            result += $"MinApprovalDelta: {transaction.MinApprovalDelta}\n";
            result += $"MinRemovalDelta: {transaction.MinRemovalDelta}\n";
            result += $"AddressAdditions:\n";
            result = transaction.AddressAdditions.Aggregate(result, (current, transactionAddressAdditions) => 
                current + $"\t{Converter.AddressToString(transactionAddressAdditions.bytes)}\n");
            result += $"AddressDeletions:\n";
            result = transaction.AddressAdditions.Aggregate(result, (current, transactionAddressDeletions) => 
                current + $"\t{Converter.AddressToString(transactionAddressDeletions.bytes)}\n");
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseNamespaceMetadataTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedNamespaceMetadataTransactionV1) _transaction;
            result += "NamespaceMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"TargetNamespaceId: {transaction.TargetNamespaceId.Value:X16}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
        }
        else
        {
            var transaction = (NamespaceMetadataTransactionV1) _transaction;
            result += "NamespaceMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"TargetNamespaceId: {transaction.TargetNamespaceId.Value:X16}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseMosaicMetadataTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicMetadataTransactionV1) _transaction;
            result += "MosaicMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"TargetMosaicId: {transaction.TargetMosaicId.Value:X16}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
        }
        else
        {
            var transaction = (MosaicMetadataTransactionV1) _transaction;
            result += "MosaicMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"TargetMosaicId: {transaction.TargetMosaicId.Value:X16}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseAccountMetadataTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAccountMetadataTransactionV1) _transaction;
            result += "AccountMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
        }
        else
        {
            var transaction = (AccountMetadataTransactionV1) _transaction;
            result += "AccountMetadataTransaction\n";
            result += $"TargetAddress: {Converter.AddressToString(transaction.TargetAddress.bytes)}\n";
            result += $"ScopedMetadataKey: {transaction.ScopedMetadataKey:X16}\n";
            result += $"ValueSizeDelta: {transaction.ValueSizeDelta}\n";
            result += $"Value: {Encoding.UTF8.GetString(transaction.Value)}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseSecretProofTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedSecretProofTransactionV1) _transaction;
            result += "SecretProofTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Secret: {transaction.Secret}\n";
            result += $"Proof: {Converter.BytesToHex(transaction.Proof)}\n";
            result += $"HashAlgorithm: {transaction.HashAlgorithm}\n";
        }
        else
        {
            var transaction = (SecretProofTransactionV1) _transaction;
            result += "SecretProofTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Secret: {transaction.Secret}\n";
            result += $"Proof: {Converter.BytesToHex(transaction.Proof)}\n";
            result += $"HashAlgorithm: {transaction.HashAlgorithm}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseSecretLockTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedSecretLockTransactionV1) _transaction;
            result += "SecretLockTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Secret: {transaction.Secret}\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"HashAlgorithm: {transaction.HashAlgorithm}\n";
        }
        else
        {
            var transaction = (SecretLockTransactionV1) _transaction;
            result += "SecretLockTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Secret: {transaction.Secret}\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"HashAlgorithm: {transaction.HashAlgorithm}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseHashLockTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedHashLockTransactionV1) _transaction;
            result += "HashLockTransaction\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
            result += $"Hash: {transaction.Hash}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
        }
        else
        {
            var transaction = (HashLockTransactionV1) _transaction;
            result += "HashLockTransaction\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
            result += $"Hash: {transaction.Hash}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseVrfKeyLinkTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedVrfKeyLinkTransactionV1) _transaction;
            result += "VrfKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
        }
        else
        {
            var transaction = (VrfKeyLinkTransactionV1) _transaction;
            result += "VrfKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseVotingKeyLinkTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedVotingKeyLinkTransactionV1) _transaction;
            result += "VotingKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
            result += $"StartEpoch: {transaction.StartEpoch.Value}\n";
            result += $"EndEpoch: {transaction.EndEpoch.Value}\n";
        }
        else
        {
            var transaction = (VotingKeyLinkTransactionV1) _transaction;
            result += "VotingKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
            result += $"StartEpoch: {transaction.StartEpoch.Value}\n";
            result += $"EndEpoch: {transaction.EndEpoch.Value}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseNodeKeyLinkTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedNodeKeyLinkTransactionV1) _transaction;
            result += "NodeKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
        } 
        else
        {
            var transaction = (NodeKeyLinkTransactionV1) _transaction;
            result += "NodeKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseAccountKeyLinkTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedAccountKeyLinkTransactionV1) _transaction;
            result += "AccountKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
        }
        else
        {
            var transaction = (AccountKeyLinkTransactionV1) _transaction;
            result += "AccountKeyLinkTransaction\n";
            result += $"LinkedPublicKey: {transaction.LinkedPublicKey}\n";
            result += $"LinkAction: {transaction.LinkAction}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseMosaicSupplyRevocationTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicSupplyRevocationTransactionV1) _transaction;
            result += "MosaicSupplyRevocationTransaction\n";
            result += $"SourceAddress: {Converter.AddressToString(transaction.SourceAddress.bytes)}\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
        }
        else
        {
            var transaction = (MosaicSupplyRevocationTransactionV1) _transaction;
            result += "MosaicSupplyRevocationTransaction\n";
            result += $"SourceAddress: {Converter.AddressToString(transaction.SourceAddress.bytes)}\n";
            result += $"MosaicID: {transaction.Mosaic.MosaicId}\n";
            result += $"Amount: {transaction.Mosaic.Amount.Value}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        
        return result;
    }

    static string ParseMosaicSupplyChangeTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicSupplyChangeTransactionV1) _transaction;
            result += "MosaicDefinitionTransaction\n";
            result += $"MosaicID: {transaction.MosaicId.Value:X16}\n";
            result += $"Action: {transaction.Action}\n";
            result += $"Delta: {transaction.Delta.Value}\n";
        }
        else
        {
            var transaction = (MosaicSupplyChangeTransactionV1) _transaction;
            result += "MosaicDefinitionTransaction\n";
            result += $"MosaicID: {transaction.MosaicId.Value:X16}\n";
            result += $"Action: {transaction.Action}\n";
            result += $"Delta: {transaction.Delta.Value}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        return result;
    }

    static string ParseMosaicDefinitionTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedMosaicDefinitionTransactionV1) _transaction;
            result += "MosaicDefinitionTransaction\n";
            result += $"MosaicID: {transaction.Id.Value:X16}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"Divisibility: {transaction.Divisibility}\n";
            result += $"MosaicFlags: {ParseMosaicFlags(transaction.Flags)}\n";
        }
        else
        {
            var transaction = (MosaicDefinitionTransactionV1) _transaction;
            result += "MosaicDefinitionTransaction\n";
            result += $"MosaicID: {transaction.Id.Value:X16}\n";
            result += $"Duration: {transaction.Duration.Value}\n";
            result += $"Divisibility: {transaction.Divisibility}\n";
            result += $"MosaicFlags: {ParseMosaicFlags(transaction.Flags)}\n";
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        return result;
    }

    static string ParseMosaicFlags(MosaicFlags flags)
    {
        var result = "\n";
        var supplyMutable = (flags.Value & (1 << 0)) != 0;
        result += supplyMutable ? "\tSupplyMutable: true\n" : "\tSupplyMutable: false\n";
        var transferable = (flags.Value & (1 << 1)) != 0;
        result += transferable ? "\tTransferable: true\n" : "\tTransferable: false\n";
        var restrictable = (flags.Value & (1 << 2)) != 0;
        result += restrictable ? "\tRestrictable: true\n" : "\tRestrictable: false\n";
        var revokable = (flags.Value & (1 << 3)) != 0;
        result += revokable ? "\tRevokable: true" : "\tRevokable: false";
        return result;
    }

    static string ParseTransferTransaction(IBaseTransaction _transaction, bool embedded = false)
    {
        var result = "";
        if (embedded)
        {
            var transaction = (EmbeddedTransferTransactionV1) _transaction;
            result += "TransferTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Message: {ParseMessage(transaction.Message)}\n";
            result += "Mosaics:\n";
            foreach (var transferTransactionMosaic in transaction.Mosaics)
            {
                result += $"\tMosaicID: {transferTransactionMosaic.MosaicId}\n";
                result += $"\t\tAmount: {transferTransactionMosaic.Amount.Value.ToString()}\n";
            }
        }
        else
        {
            var transaction = (TransferTransactionV1) _transaction;
            result += "TransferTransaction\n";
            result += $"RecipientAddress: {Converter.AddressToString(transaction.RecipientAddress.bytes)}\n";
            result += $"Message: {ParseMessage(transaction.Message)}\n";
            result += "Mosaics:\n";
            foreach (var transferTransactionMosaic in transaction.Mosaics)
            {
                result += $"\tMosaicID: {transferTransactionMosaic.MosaicId}\n";
                result += $"\t\tAmount: {transferTransactionMosaic.Amount.Value.ToString()}\n";
            }
            result += ParseTransactionBase(transaction.Network, transaction.Deadline, transaction.Fee);
        }
        return result;
    }
    
    static string ParseTransactionBase(NetworkType networkType, Timestamp deadline, Amount fee)
    {
        var result = "";
        result += $"Network: {ParseNetworkType(networkType)}\n";
        result += $"Deadline: {ParseDeadline(deadline)}\n";
        result += $"MaxFee: {ParseFee(fee)}\n";
        return result;
    }

    static string ParseNetworkType(NetworkType networkType)
    {
        return networkType.Value switch
        {
            152 => "TestNet",
            104 => "MainNet",
            _ => networkType.Value.ToString()
        };
    }

    static string ParseFee(Amount fee)
    {
        return ((double)fee.Value / 1000000).ToString(CultureInfo.InvariantCulture);
    }

    static string ParseDeadline(BaseValue timestamp)
    {
        if (CatSdk.Symbol.Network.TestNet.epocTime == null) throw new NullReferenceException("ネットワークのエポックタイムが設定されていません");
        var timeSpan = TimeSpan.FromSeconds(timestamp.Value);
        var deadline = CatSdk.Symbol.Network.TestNet.epocTime.Value.Add(timeSpan / 1000);
        return deadline.ToString(CultureInfo.InvariantCulture);
    }

    static string ParseMessage(byte[] bytes)
    {
        if(bytes.Length == 0) return "";
        switch (bytes[0])
        {
            case 0:
            {
                var message = Encoding.UTF8.GetString(bytes);
                return message;
            }
            case 1:
                return "暗号化されたメッセージです";
            default:
                return Converter.BytesToHex(bytes);
        }
    }
}