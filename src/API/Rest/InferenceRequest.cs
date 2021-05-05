﻿/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Ardalis.GuardClauses;
using Newtonsoft.Json;
using Nvidia.Clara.DicomAdapter.Common;
using Nvidia.Clara.Platform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nvidia.Clara.DicomAdapter.API.Rest
{
    /// <summary>
    /// Status of an inference request.
    /// </summary>
    public enum InferenceRequestStatus
    {
        Unknown,
        Success,
        Fail
    }

    /// <summary>
    /// State of a inference request.
    /// </summary>
    public enum InferenceRequestState
    {
        /// <summary>
        /// Indicates that an inference request is currently queued for data retrieval.
        /// </summary>
        Queued,

        /// <summary>
        /// The inference request is being processing by DICOM Adapter.
        /// </summary>
        InProcess,

        /// <summary>
        /// Indicates DICOM Adapter has submitted a new pipeline job with the Clara Platform.
        /// </summary>
        Completed,
    }

    /// <summary>
    /// Structure that represents an inference request based on ACR's Platform-Model Communication for AI.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "transactionID": "ABCDEF123456",
    ///     "priority": "255",
    ///     "inputMetadata": { ... },
    ///     "inputResources": [ ... ],
    ///     "outputResources": [ ... ]
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Refer to [ACR DSI Model API](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf)
    /// for more information.
    /// <para><c>transactionID></c> is required.</para>
    /// <para><c>inputMetadata></c> is required.</para>
    /// <para><c>inputResources></c> is required.</para>
    /// </remarks>
    public class InferenceRequest
    {
        /// <summary>
        /// Gets or set the transaction ID of a request.
        /// </summary>
        [JsonProperty(PropertyName = "transactionID")]
        public string TransactionId { get; set; }

        /// <summary>
        /// Gets or sets the priority of a request.
        /// </summary>
        /// <remarks>
        /// <para>Default value is <c>128</c> which maps to <c>JOB_PRIORITY_NORMAL</c>.</para>
        /// <para>Any value lower than <c>128</c> is map to <c>JOB_PRIORITY_LOWER</c>.</para>
        /// <para>Any value between <c>129-254</c> (inclusive) is set to <c>JOB_PRIORITY_HIGHER</c>.</para>
        /// <para>Value of <c>255</c> maps to <c>JOB_PRIORITY_IMMEDIATE</c>.</para>
        /// </remarks>
        [JsonProperty(PropertyName = "priority")]
        public byte Priority { get; set; } = 128;

        /// <summary>
        /// Gets or sets the details of the data associated with the inference request.
        /// </summary>
        [JsonProperty(PropertyName = "inputMetadata")]
        public InferenceRequestMetadata InputMetadata { get; set; }

        /// <summary>
        /// Gets or set a list of data sources to query/retrieve data from.
        /// When multiple data sources are specified, the system will query based on
        /// the order the list was received.
        /// </summary>
        [JsonProperty(PropertyName = "inputResources")]
        public IList<RequestInputDataResource> InputResources { get; set; }

        /// <summary>
        /// Gets or set a list of data sources to export results to.
        /// In order to export via DICOMweb, the Clara Pipeline must include
        /// and use Register Results Operator and register the results with agent
        /// name "DICOMweb" or the values configured in dicom>scu>export>agent field.
        /// </summary>
        [JsonProperty(PropertyName = "outputResources")]
        public IList<RequestOutputDataResource> OutputResources { get; set; }

        #region Internal Use Only

        /// <summary>
        /// Unique identity for the request.
        /// </summary>
        public Guid InferenceRequestId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Internal use - gets or sets the Job ID for the request once	        public Guid InferenceRequestId { get; set; } = Guid.NewGuid();
        /// the job is created with Clara Platform Jobs API.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "jobId")]
        public string JobId { get; set; }

        /// <summary>	
        /// Internal use only - get or sets the Payload ID for the request once	
        /// the job is created with Clara Platform Jobs API.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "payloadId")]
        public string PayloadId { get; set; }

        /// <summary>	
        /// Internal use only - get or sets the state of a inference request.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "state")]
        public InferenceRequestState State { get; set; } = InferenceRequestState.Queued;

        /// <summary>	
        /// Internal use only - get or sets the status of a inference request.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "status")]
        public InferenceRequestStatus Status { get; set; } = InferenceRequestStatus.Unknown;

        /// <summary>	
        /// Internal use only - get or sets the status of a inference request.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "storagePath")]
        public string StoragePath { get; set; }

        /// <summary>	
        /// Internal use only - get or sets number of retries performed.	
        /// </summary>	
        /// <remarks>	
        /// Internal use only.	
        /// </remarks>	
        [JsonProperty(PropertyName = "tryCount")]
        public int TryCount { get; set; } = 0;

        [JsonIgnore]
        public InputConnectionDetails Algorithm
        {
            get
            {
                return InputResources.FirstOrDefault(predicate => predicate.Interface == InputInterfaceType.Algorithm)?.ConnectionDetails;
            }
        }

        [JsonIgnore]
        public JobPriority ClaraJobPriority
        {
            get
            {
                switch (Priority)
                {
                    case byte n when (n < 128):
                        return JobPriority.Lower;

                    case byte n when (n == 128):
                        return JobPriority.Normal;

                    case byte n when (n == 255):
                        return JobPriority.Immediate;

                    default:
                        return JobPriority.Higher;
                }
            }
        }

        [JsonIgnore]
        public string JobName
        {
            get
            {
                return $"{TransactionId}-{Algorithm.Name}-{DateTime.UtcNow:yyyyMMddHHmmss}".FixJobName();
            }
        }
        #endregion

        public InferenceRequest()
        {
            InputResources = new List<RequestInputDataResource>();
            OutputResources = new List<RequestOutputDataResource>();
        }

        /// <summary>
        /// Configures temporary storage location used to store retrieved data.
        /// </summary>
        /// <param name="temporaryStorageRoot">Root path to the temporary storage location.</param>
        public void ConfigureTemporaryStorageLocation(string storagePath)
        {
            Guard.Against.NullOrWhiteSpace(storagePath, nameof(storagePath));
            if (!string.IsNullOrWhiteSpace(StoragePath))
            {
                throw new InferenceRequestException("StoragePath already configured.");
            }

            StoragePath = storagePath;
        }

        public bool IsValid(out string details)
        {
            Preprocess();
            return Validate(out details);
        }

        private void Preprocess()
        {
            if (InputMetadata.Inputs is null)
            {
                InputMetadata.Inputs = new List<InferenceRequestDetails>();
            }

            if (!(InputMetadata.Details is null))
            {
                InputMetadata.Inputs.Add(InputMetadata.Details);
                InputMetadata.Details = null;
            }
        }

        private bool Validate(out string details)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TransactionId))
            {
                errors.Add("'transactionId' is required.");
            }

            if (InputResources.IsNullOrEmpty() ||
                InputResources.Count(predicate => predicate.Interface != InputInterfaceType.Algorithm) == 0)
            {
                errors.Add("No 'intputResources' specified.");
            }

            if (Algorithm is null)
            {
                errors.Add("No algorithm defined or more than one algorithms defined in 'inputResources'.  'inputResources' must include one algorithm/pipeline for the inference request.");
            }

            if (InputMetadata is null || (InputMetadata.Details is null && InputMetadata.Inputs.IsNullOrEmpty()))
            {
                errors.Add("Request has no `inputMetadata` defined. At least one `inputs` or `inputMetadata` required.");
            }
            else
            {
                if (!(InputMetadata.Details is null))
                {
                    CheckInputMetadataDetails(InputMetadata.Details, errors);
                }
                if (!(InputMetadata.Inputs is null))
                {
                    foreach (var inputDetails in InputMetadata.Inputs)
                    {
                        CheckInputMetadataDetails(inputDetails, errors);
                    }
                }
            }

            foreach (var input in InputResources)
            {
                if (input.Interface == InputInterfaceType.DicomWeb)
                {
                    CheckDicomWebConnectionDetails("inputResources", errors, input.ConnectionDetails);
                }
                else if (input.Interface == InputInterfaceType.Fhir)
                {
                    CheckFhirConnectionDetails("inputResources", errors, input.ConnectionDetails);
                }
            }

            foreach (var output in OutputResources)
            {
                if (output.Interface == InputInterfaceType.DicomWeb)
                {
                    CheckDicomWebConnectionDetails("outputResources", errors, output.ConnectionDetails);
                }
                else if (output.Interface == InputInterfaceType.Fhir)
                {
                    CheckFhirConnectionDetails("outputResources", errors, output.ConnectionDetails);
                }
            }

            details = string.Join(' ', errors);
            return errors.Count == 0;
        }

        private void CheckInputMetadataDetails(InferenceRequestDetails details, List<string> errors)
        {
            switch (details.Type)
            {
                case InferenceRequestType.DicomUid:
                    if (details.Studies.IsNullOrEmpty())
                    {
                        errors.Add("Request type is set to `DICOM_UID` but no `studies` defined.");
                    }
                    else
                    {
                        foreach (var study in details.Studies)
                        {
                            if (string.IsNullOrWhiteSpace(study.StudyInstanceUid))
                            {
                                errors.Add("`StudyInstanceUID` cannot be empty.");
                            }

                            if (study.Series is null) continue;

                            foreach (var series in study.Series)
                            {
                                if (string.IsNullOrWhiteSpace(series.SeriesInstanceUid))
                                {
                                    errors.Add("`SeriesInstanceUID` cannot be empty.");
                                }

                                if (series.Instances is null) continue;

                                foreach (var instance in series.Instances)
                                {
                                    if (instance.SopInstanceUid.Any(p => string.IsNullOrWhiteSpace(p)))
                                    {
                                        errors.Add("`SOPInstanceUID` cannot be empty.");
                                    }
                                }
                            }
                        }
                    }
                    break;

                case InferenceRequestType.DicomPatientId:
                    if (string.IsNullOrWhiteSpace(details.PatientId))
                    {
                        errors.Add("Request type is set to `DICOM_PATIENT_ID` but `PatientID` is not defined.");
                    }
                    break;

                case InferenceRequestType.AccessionNumber:
                    if (details.AccessionNumber.IsNullOrEmpty())
                    {
                        errors.Add("Request type is set to `ACCESSION_NUMBER` but no `accessionNumber` defined.");
                    }
                    break;

                case InferenceRequestType.FhireResource:
                    if (details.Resources.IsNullOrEmpty())
                    {
                        errors.Add("Request type is set to `FHIR_RESOURCE` but no FHIR `resources` defined.");
                    }
                    else
                    {
                        foreach (var resource in details.Resources)
                        {
                            if (string.IsNullOrWhiteSpace(resource.Type))
                            {
                                errors.Add("A FHIR resource type cannot be empty.");
                            }
                        }
                    }
                    break;

                default:
                    errors.Add($"'inputMetadata' does not yet support type '{details.Type}'.");
                    break;
            }
        }

        private void CheckFhirConnectionDetails(string source, List<string> errors, DicomWebConnectionDetails connection)
        {
            if (!Uri.IsWellFormedUriString(connection.Uri, UriKind.Absolute))
            {
                errors.Add($"The provided URI '{connection.Uri}' is not well formed.");
            }
        }

        private static void CheckDicomWebConnectionDetails(string source, List<string> errors, DicomWebConnectionDetails connection)
        {
            if (connection.AuthType != ConnectionAuthType.None && string.IsNullOrWhiteSpace(connection.AuthId))
            {
                errors.Add($"One of the '{source}' has authType of '{connection.AuthType:F}' but does not include a valid value for 'authId'");
            }

            if (!Uri.IsWellFormedUriString(connection.Uri, UriKind.Absolute))
            {
                errors.Add($"The provided URI '{connection.Uri}' is not well formed.");
            }
        }
    }
}