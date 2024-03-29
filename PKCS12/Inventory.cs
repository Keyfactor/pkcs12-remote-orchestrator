﻿// Copyright 2021 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Newtonsoft.Json.Linq;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Common.Enums;

using Org.BouncyCastle.Pkcs;

using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.PKCS12
{
    public class Inventory : IInventoryJobExtension
    {
        public string ExtensionName => "";

        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            ILogger logger = LogHandler.GetClassLogger<Inventory>();
            logger.LogDebug($"Begin PKCS12 Inventory job for job id {config.JobId}...");

            PKCS12Store pkcs12Store = new PKCS12Store(config.CertificateStoreDetails.ClientMachine, config.ServerUsername, config.ServerPassword, config.CertificateStoreDetails.StorePath, config.CertificateStoreDetails.StorePassword);

            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();
            try
            {
                ApplicationSettings.Initialize(this.GetType().Assembly.Location);

                pkcs12Store.Initialize();

                List<X509Certificate2Collection> collections = pkcs12Store.GetCertificateChains();

                foreach (X509Certificate2Collection collection in collections)
                {
                    if (collection.Count == 0)
                        continue;

                    X509Certificate2Ext issuedCertificate = (X509Certificate2Ext)collection[0];

                    List<string> certChain = new List<string>();
                    foreach (X509Certificate2 certificate in collection)
                    {
                        certChain.Add(Convert.ToBase64String(certificate.Export(X509ContentType.Cert)));
                        logger.LogDebug(Convert.ToBase64String(certificate.Export(X509ContentType.Cert)));
                    }

                    inventoryItems.Add(new CurrentInventoryItem()
                    {
                        ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                        Alias = string.IsNullOrEmpty(issuedCertificate.FriendlyNameExt) ? issuedCertificate.Thumbprint : issuedCertificate.FriendlyNameExt,
                        PrivateKeyEntry = issuedCertificate.HasPrivateKey,
                        UseChainLevel = collection.Count > 1,
                        Certificates = certChain.ToArray()
                    });
                }  
            }
            catch (Exception ex)
            {
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = ExceptionHandler.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}:") };
            }
            finally
            {
                pkcs12Store.Terminate();
            }

            try
            {
                submitInventory.Invoke(inventoryItems);
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
            }
            catch (Exception ex)
            {
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = ExceptionHandler.FlattenExceptionMessages(ex, $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}:") };
            }
        }
    }
}