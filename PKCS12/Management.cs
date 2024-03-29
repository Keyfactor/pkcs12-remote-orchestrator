﻿// Copyright 2021 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.IO;
using System.Linq;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Common.Enums;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.PKCS12
{
    public class Management : IManagementJobExtension
    {
        public string ExtensionName => "";

        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            ILogger logger = LogHandler.GetClassLogger<Inventory>();
            logger.LogDebug($"Begin PKCS12 Management-{Enum.GetName(typeof(CertStoreOperationType), config.OperationType)} job for job id {config.JobId}...");

            PKCS12Store pkcs12Store = new PKCS12Store(config.CertificateStoreDetails.ClientMachine, config.ServerUsername, config.ServerPassword, config.CertificateStoreDetails.StorePath, config.CertificateStoreDetails.StorePassword);

            try
            {
                ApplicationSettings.Initialize(this.GetType().Assembly.Location);

                bool hasPassword = !string.IsNullOrEmpty(config.JobCertificate.PrivateKeyPassword);
                pkcs12Store.Initialize();

                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        logger.LogDebug($"Begin Create Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (!pkcs12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {pkcs12Store.StorePath + pkcs12Store.StoreFileName} does not exist on server {pkcs12Store.Server}.");
                        }
                        else
                        {
                            pkcs12Store.AddCertificate(config.JobCertificate.Alias, config.JobCertificate.Contents, config.Overwrite, config.JobCertificate.PrivateKeyPassword);
                        }
                        break;

                    case CertStoreOperationType.Remove:
                        logger.LogDebug($"Begin Delete Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (!pkcs12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {pkcs12Store.StorePath + pkcs12Store.StoreFileName} does not exist on server {pkcs12Store.Server}.");
                        }
                        else
                        {
                            pkcs12Store.DeleteCertificateByAlias(config.JobCertificate.Alias);
                        }
                        break;

                    case CertStoreOperationType.Create:
                        logger.LogDebug($"Begin Create Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (pkcs12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {pkcs12Store.StorePath + pkcs12Store.StoreFileName} already exists.");
                        }
                        else
                        {
                            dynamic properties = JsonConvert.DeserializeObject(config.CertificateStoreDetails.Properties.ToString());
                            string linuxFilePermissions = properties.linuxFilePermissionsOnStoreCreation == null || string.IsNullOrEmpty(properties.linuxFilePermissionsOnStoreCreation.Value) ? 
                                ApplicationSettings.DefaultLinuxPermissionsOnStoreCreation :
                                properties.linuxFilePermissionsOnStoreCreation.Value;

                            pkcs12Store.CreateCertificateStore(config.CertificateStoreDetails.StorePath, linuxFilePermissions);
                        }
                        break;

                    default:
                        return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: Unsupported operation: {config.OperationType.ToString()}" };
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

            return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
        }
    }
}