// Copyright 2021 Keyfactor
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

using Org.BouncyCastle.Pkcs;

namespace Keyfactor.Extensions.Orchestrator.PKCS12
{
    public class Management : IManagementJobExtension
    {
        public string ExtensionName => "";

        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            ILogger logger = LogHandler.GetClassLogger<Inventory>();
            logger.LogDebug($"Begin PKCS12 Management-{Enum.GetName(typeof(CertStoreOperationType), config.OperationType)} job for job id {config.JobId}...");

            PKCS12Store PKCS12Store = new PKCS12Store(config.CertificateStoreDetails.ClientMachine, config.ServerUsername, config.ServerPassword, config.CertificateStoreDetails.StorePath, config.CertificateStoreDetails.StorePassword);

            try
            {
                ApplicationSettings.Initialize(this.GetType().Assembly.Location);

                bool hasPassword = !string.IsNullOrEmpty(config.JobCertificate.PrivateKeyPassword);
                PKCS12Store.Initialize();

                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        logger.LogDebug($"Begin Create Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (!PKCS12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {PKCS12Store.StorePath + PKCS12Store.StoreFileName} does not exist on server {PKCS12Store.Server}.");
                        }
                        else
                        {
                            PKCS12Store.AddCertificate(config.JobCertificate.Alias, config.JobCertificate.Contents, config.Overwrite, config.JobCertificate.PrivateKeyPassword);
                        }
                        break;

                    case CertStoreOperationType.Remove:
                        logger.LogDebug($"Begin Delete Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (!PKCS12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {PKCS12Store.StorePath + PKCS12Store.StoreFileName} does not exist on server {PKCS12Store.Server}.");
                        }
                        else
                        {
                            PKCS12Store.DeleteCertificateByAlias(config.JobCertificate.Alias);
                        }
                        break;

                    case CertStoreOperationType.Create:
                        logger.LogDebug($"Begin Create Operation for {config.CertificateStoreDetails.StorePath} on {config.CertificateStoreDetails.ClientMachine}.");
                        if (PKCS12Store.DoesStoreExist())
                        {
                            throw new PKCS12Exception($"Certificate store {PKCS12Store.StorePath + PKCS12Store.StoreFileName} already exists.");
                        }
                        else
                        {
                            PKCS12Store.CreateCertificateStore(config.CertificateStoreDetails.StorePath, config.CertificateStoreDetails.StorePassword);
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
                PKCS12Store.Terminate();
            }

            return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
        }
    }
}