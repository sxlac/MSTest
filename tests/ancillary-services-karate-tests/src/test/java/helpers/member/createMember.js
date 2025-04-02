// This helper allows us to try to create a member up to three times
function createMember() {
    var DataGen = function() { var DataGen = Java.type('helpers.data.DataGen'); return new DataGen(); }
    var Faker = function() { var Faker = Java.type('helpers.data.Faker'); return new Faker(); }

    for (i = 0; i < 3; i++) {

        try {
            var censeoId = `${Faker().randomCenseoId(7)}`
            var externalId = DataGen().uuid()
    
            var createMemberRequestObjs = { censeoId: censeoId, externalId: externalId }
            var memberDetails = karate.call('classpath:helpers/member/createMember.feature', createMemberRequestObjs).memberDetails
    
            if (typeof memberDetails.memberId != undefined && memberDetails.memberId != null) {
                return memberDetails
            }
        } catch (err) {
            karate.log('Unable to create a member')
        }   
    }
    karate.fail('Unable to create a member to test with!')
}

