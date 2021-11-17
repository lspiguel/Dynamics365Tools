
import os
import json
from urllib.parse import urlparse, parse_qs
from dynamics365crm.client import Client

import dynamics365ce_helpers

def UserSaidYes(text):
	while True:
		response = input(text + " (y/n) ")
		if response in ["Y", "y"]:
			return True
		elif response in ["N", "n"]:
			return False
		else:
			print("Sorry, didn't get that")

resource = input("Dynamics 365 URL? ")

token = dynamics365ce_helpers.GetInteractiveToken(resource)

client = Client(resource, token=token)
matricula = input("matricula?")

while True:
    if UserSaidYes("Update?"):
        for x in client.get_data(type="account")['value']:
            if x['axx_matricula'] == matricula:
                print("Old values:", x['accountid'], x['axx_matricula'], x['axx_nombreapellido'], x['statecode'], x['statuscode'], x['address1_postalcode'])
                client.update_lead(x['accountid'], statuscode = 1, address1_postalcode = '666')
                break

    for x in client.get_data(type="account")['value']:
        if x['axx_matricula'] == matricula:
            print("Values now:", x['accountid'], x['axx_matricula'], x['axx_nombreapellido'], x['statecode'], x['statuscode'], x['address1_postalcode'])
            break

    if UserSaidYes("\r\nAgain?"):
        continue
    else:
        break

