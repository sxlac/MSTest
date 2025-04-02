using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Signify.A1C.Svc.Core.Tests.Utilities
{
    public static class ContentHelper
    {
        public const string ProviderResponse = @"{
                                                'providerId': 42879,
                                                'gender': 1,
                                                'providerStatusId': 1,
                                                'providerStatusName': 'Active',
                                                'credentialStatusId': 2,
                                                'credentialStatusName': 'Approved',
                                                'firstName': 'Guava',
                                                'lastName': 'Inhome',
                                                'dateOfBirth': '1948-03-01T00:00:00',
                                                'primaryPhone': '2145555151',
                                                'secondaryPhone': null,
                                                'nationalProviderIdentifier': '9230239051',
                                                'personalEmail': 'GuavaInhome@dev.signifyhealth.com',
                                                'averageEvaluationTimeInMinutes': 60,
                                                'latitude': 29.956682,
                                                'longitude': -90.073501,
                                                'addressLineOne': '701 SW 30th St',
                                                'addressLineTwo': 'Suite 100',
                                                'city': 'Oklahoma City',
                                                'state': 'OK',
                                                'specialization': 'Surgery',
                                                'subSpecialty': null,
                                                'defaultHotelNotes': null,
                                                'defaultRentalCarNotes': null,
                                                'defaultFlightNotes': null,
                                                'terminationDate': null,
                                                'createDate': '2020-06-20T11:02:58.617',
                                                'dateLastUpdated': '2020-09-03T10:05:01.307',
                                                'onlineTrainingCompletedCreateDate': null,
                                                'isOnlineTrainingCompleted': false,
                                                'qatrainingCompletedCreateDate': null,
                                                'notInOfficeofInspectorGeneralExclusionListCreateDate': '2014-06-03T11:16:25.053',
                                                'isNotInOfficeofInspectorGeneralExclusionList': true,
                                                'notInGeneralServicesAdministrationExclusionListCreateDate': '2014-06-03T11:16:25.053',
                                                'isNotInGeneralServicesAdministrationExclusionList': true,
                                                'zipCode': '73109',
                                                'socialSecurityNumber': '000-00-0000',
                                                'degree': 'DO',
                                                'hasSignedDevicePolicy': false,
                                                'devicePolicySignedDate': null,
                                                'isActiveMobileUser': true,
                                                'mobileDeviceNotes': null,
                                                'hasCenseoiPad': true,
                                                'isPendingHardwareReturn': false,
                                                'thirdPartyManagementId': null,
                                                'middleName': 's',
                                                'useDrivingMinutes': true,
                                                'censeoHealthEmail': null,
                                                'modifiedBy': 5948,
                                                'employeeType': 3,
                                                'recruiterInTrainingOktaUserId': null,
                                                'recruiterOktaUserId': null,
                                                'recruiterUserId': 1,
                                                'recruiterInTrainingId': 1,
                                                'oktaUserId': null,
                                                'primaryLanguage': 'English',
                                                'secondaryLanguage': 'None',
                                                'licensedStates': [
                                                    {
                                                        'licenseNumber': 'A1111111',
                                                        'dateIssued': null,
                                                        'isLicenseRestricted': false,
                                                        'licenseRestrictedReason': null,
                                                        'controlPermitNumber': null,
                                                        'controlPermitDateIssued': null,
                                                        'controlPermitNumberExpirationDate': null,
                                                        'notInStateOptOutList': true,
                                                        'dateVerifiedNotInStateOptOutList': '2014-05-14T00:00:00',
                                                        'stateId': 36,
                                                        'abbreviation': 'OK',
                                                        'expirationDate': '2021-08-26T13:14:05',
                                                        'isCurrent': true
                                                    }
                                                ],
                                                'recruiter': null,
                                                'recruiterTraining': null,
                                                'modifiedByName': 'Atul Patel',
                                                'thirdPartyManagementName': null,
                                                'dateMailedBadge': '2014-11-07T00:00:00',
                                                'badgePhotoReceived': true
                                            }";

        public const string MemberResponse = @"{
                                                'AddressLineOne': '4420 Harpers Ferry Dr',
                                                'AddressLineTwo': '',
                                                'City': 'Grand Prairie',
                                                'Client': 'BCBS Tennessee old ',
                                                'DateOfBirth': '6/24/1960 6:25:49 PM',
                                                'FirstName': 'TestName1',
                                                'LastName': 'TestLastName2',
                                                'MiddleName': null,
                                                'State': 'TX',
                                                'ZipCode': '75052'
                                            }";

        public const string InventoryResponse = @"{
                                                    'requestId': 'ab68bb47-438c-47b7-895e-6758013bd30a',
                                                    'success': true,
                                                    'message': null
                                                }";

        public const string AccessToken = @"{
                                                'access_token': '12345',
                                                'expires_in': 10
                                            }";

        public const string InventoryUpdateRequest = @"{
                                                        'correlationId': 'ab68bb47-438c-47b7-895e-6758013bd30a',
                                                        'requestId': 'ab68bb47-438c-47b7-895e-6758013bd30a',
                                                        'a1CId': 500,
                                                        'itemNumber': null,
                                                        'dateUpdated': '0001-01-01T00:00:00',
                                                        'serialNumber': null,
                                                        'quantity': 1,
                                                        'providerId': 0,
                                                        'customerNumber': null,
                                                        'evaluationId': 0
                                                    }";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static StringContent GetStringContent(object obj)
            => new StringContent(JsonConvert.SerializeObject(obj), Encoding.Default, "application/json");
    }
}
