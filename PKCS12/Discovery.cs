// Copyright 2021 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Common.Enums;

using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.PKCS12
{
    public class Discovery : IDiscoveryJobExtension
    {
        public string ExtensionName => "";

        public JobResult ProcessJob(DiscoveryJobConfiguration config, SubmitDiscoveryUpdate submitDiscovery)
        {
            ILogger logger = LogHandler.GetClassLogger<Discovery>();
            logger.LogDebug($"Begin PKCS12 Discovery job for job id {config.JobId}...");

            List<string> locations = new List<string>();
            string server = string.Empty;

            string[] directoriesToSearch = config.JobProperties["dirs"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] extensionsToSearch = config.JobProperties["extensions"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] ignoredDirs = config.JobProperties["ignoreddirs"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] filesTosearch = config.JobProperties["patterns"].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            PKCS12Store pkcs12Store = new PKCS12Store(config.ClientMachine, config.ServerUsername, config.ServerPassword, directoriesToSearch[0].Substring(0, 1) == "/" ? PKCS12Store.ServerTypeEnum.Linux : PKCS12Store.ServerTypeEnum.Windows);

            try
            {
                ApplicationSettings.Initialize(this.GetType().Assembly.Location);

                if (directoriesToSearch.Length == 0)
                    throw new PKCS12Exception("Blank or missing search directories for Discovery.");
                if (extensionsToSearch.Length == 0)
                    throw new PKCS12Exception("Blank or missing search extensions for Discovery.");
                if (filesTosearch.Length == 0)
                    filesTosearch = new string[] { "*" };

                pkcs12Store.Initialize();

                locations = pkcs12Store.FindStores(directoriesToSearch, extensionsToSearch, filesTosearch);
                foreach (string ignoredDir in ignoredDirs)
                    locations = locations.Where(p => !p.StartsWith(ignoredDir)).ToList();
            }
            catch (Exception ex)
            {
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = ExceptionHandler.FlattenExceptionMessages(ex, $"Server {config.ClientMachine}:") };
            }
            finally
            {
                pkcs12Store.Terminate();
            }

            try
            {
                submitDiscovery.Invoke(locations);
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
            }
            catch (Exception ex)
            {
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, FailureMessage = ExceptionHandler.FlattenExceptionMessages(ex, $"Server {config.ClientMachine}:") };
            }
        }
    }
}