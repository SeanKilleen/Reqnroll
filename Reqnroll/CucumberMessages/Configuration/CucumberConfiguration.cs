﻿using Reqnroll.CommonModels;
using Reqnroll.EnvironmentAccess;
using Reqnroll.Tracing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reqnroll.CucumberMessages.Configuration
{
    public class CucumberConfiguration : ICucumberConfiguration
    {
        private ITraceListener _trace;
        private IEnvironmentWrapper _environmentWrapper;

        private ResolvedConfiguration outputConfiguration = new();
        private bool _enablementOverrideFlag = true;

        public bool Enabled => _enablementOverrideFlag && outputConfiguration.Enabled;
        public string BaseDirectory => outputConfiguration.BaseDirectory;
        public string OutputDirectory => outputConfiguration.OutputDirectory;
        public string OutputFileName => outputConfiguration.OutputFileName;

        public CucumberConfiguration(ITraceListener traceListener, IEnvironmentWrapper environmentWrapper)
        {
            _trace = traceListener;
            _environmentWrapper = environmentWrapper;
        }

        #region Override API
        public void SetEnabled(bool value)
        {
            _enablementOverrideFlag = value;
        }
        #endregion
        
        
        public ResolvedConfiguration ResolveConfiguration()
        {
            var config = ApplyHierarchicalConfiguration();
            var resolved = ApplyEnvironmentOverrides(config);

            // a final sanity check, the filename cannot be empty
            if (string.IsNullOrEmpty(resolved.OutputFileName))
            {
                resolved.OutputFileName = "reqnroll_report.ndjson";
                _trace!.WriteToolOutput($"WARNING: Cucumber Messages: Output filename was empty. Setting filename to {resolved.OutputFileName}");
            }
            EnsureOutputDirectory(resolved);

            string logEntry;
            logEntry = $"Cucumber Messages: FileOutput Initialized. Output Path: {Path.Combine(resolved.BaseDirectory, resolved.OutputDirectory, resolved.OutputFileName)}";

            _trace!.WriteTestOutput(logEntry);
            outputConfiguration = resolved;
            return resolved;
        }
        private ConfigurationDTO ApplyHierarchicalConfiguration()
        {
            var defaultConfigurationProvider = new DefaultConfigurationSource(_environmentWrapper);
            var fileBasedConfigurationProvider = new RCM_ConfigFile_ConfigurationSource();

            ConfigurationDTO config = defaultConfigurationProvider.GetConfiguration();
            config = AddConfig(config, fileBasedConfigurationProvider.GetConfiguration());
            return config;
        }

        private ResolvedConfiguration ApplyEnvironmentOverrides(ConfigurationDTO config)
        {
            var baseOutDirValue = _environmentWrapper.GetEnvironmentVariable(CucumberConfigurationConstants.REQNROLL_CUCUMBER_MESSAGES_OUTPUT_BASE_DIRECTORY_ENVIRONMENT_VARIABLE);
            var relativePathValue = _environmentWrapper.GetEnvironmentVariable(CucumberConfigurationConstants.REQNROLL_CUCUMBER_MESSAGES_OUTPUT_RELATIVE_PATH_ENVIRONMENT_VARIABLE);
            var fileNameValue = _environmentWrapper.GetEnvironmentVariable(CucumberConfigurationConstants.REQNROLL_CUCUMBER_MESSAGES_OUTPUT_FILENAME_ENVIRONMENT_VARIABLE);
            var profileValue = _environmentWrapper.GetEnvironmentVariable(CucumberConfigurationConstants.REQNROLL_CUCUMBER_MESSAGES_ACTIVE_OUTPUT_PROFILE_ENVIRONMENT_VARIABLE);
            string profileName = profileValue is Success<string> ? ((Success<string>)profileValue).Result : "DEFAULT";

            var activeConfiguredDestination = config.Profiles.Where(d => d.ProfileName == profileName).FirstOrDefault();

            if (activeConfiguredDestination != null)
            {
                config.ActiveProfileName = profileName;
            };
            var result = new ResolvedConfiguration()
            {
                Enabled = config.FileOutputEnabled,
                BaseDirectory = config.ActiveProfile.BasePath,
                OutputDirectory = config.ActiveProfile.OutputDirectory,
                OutputFileName = config.ActiveProfile.OutputFileName
            };

            if (baseOutDirValue is Success<string>)
                result.BaseDirectory = ((Success<string>)baseOutDirValue).Result;

            if (relativePathValue is Success<string>)
                result.OutputDirectory = ((Success<string>)relativePathValue).Result;

            if (fileNameValue is Success<string>)
                result.OutputFileName = ((Success<string>)fileNameValue).Result;
            var enabledResult = _environmentWrapper.GetEnvironmentVariable(CucumberConfigurationConstants.REQNROLL_CUCUMBER_MESSAGES_ENABLE_ENVIRONMENT_VARIABLE);
            var enabled = enabledResult is Success<string> ? ((Success<string>)enabledResult).Result : "TRUE";

            result.Enabled = Convert.ToBoolean(enabled);

            return result;
        }

        private ConfigurationDTO AddConfig(ConfigurationDTO rootConfig, ConfigurationDTO overridingConfig)
        {
            if (overridingConfig != null)
            {
                foreach (var overridingProfile in overridingConfig.Profiles)
                {
                    AddOrOverrideProfile(rootConfig.Profiles, overridingProfile);
                }
                if (overridingConfig.ActiveProfileName != null && !rootConfig.Profiles.Any(p => p.ProfileName == overridingConfig.ActiveProfileName))
                {
                    // The incoming configuration DTO points to a profile that doesn't exist.
                    _trace.WriteToolOutput($"WARNING: Configuration file specifies an active profile that doesn't exist: {overridingConfig.ActiveProfileName}. Using {rootConfig.ActiveProfileName} instead.");
                }
                else if (overridingConfig.ActiveProfileName != null)
                    rootConfig.ActiveProfileName = overridingConfig.ActiveProfileName;

                rootConfig.FileOutputEnabled = overridingConfig.FileOutputEnabled;
            }

            return rootConfig;
        }

        private void AddOrOverrideProfile(List<Profile> masterList, Profile overridingProfile)
        {
            if (masterList.Any(p => p.ProfileName == overridingProfile.ProfileName))
            {
                var existingProfile = masterList.Where(p => p.ProfileName == overridingProfile.ProfileName).FirstOrDefault();

                existingProfile.BasePath = overridingProfile.BasePath;
                existingProfile.OutputDirectory = overridingProfile.OutputDirectory;
                existingProfile.OutputFileName = overridingProfile.OutputFileName;
            }
            else masterList.Add(overridingProfile);
        }

        private void EnsureOutputDirectory(ResolvedConfiguration config)
        {

            if (!Directory.Exists(config.BaseDirectory))
            {
                Directory.CreateDirectory(config.BaseDirectory);
                Directory.CreateDirectory(config.OutputDirectory);
            }
            else if (!Directory.Exists(Path.Combine(config.BaseDirectory, config.OutputDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(config.BaseDirectory, config.OutputDirectory));
            }
        }

    }
}

