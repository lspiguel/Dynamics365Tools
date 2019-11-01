
import os
import json
from urllib.parse import urlparse, parse_qs
from dynamics365crm.client import Client

import GetD365Token

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

token = GetD365Token.GetInteractiveToken(resource)

client = Client(resource, token=token)
fullname = 'El Proceso Completo SA'

while True:
    if UserSaidYes("Update?"):
        for x in client.get_data(type="leads")['value']:
            if x['fullname'] == fullname:
                print("Old values:", x['leadid'], x['fullname'], x['statecode'], x['statuscode'], x['axx_aprobacionabm'])
                client.update_lead(x['leadid'], statuscode = 1, axx_aprobacionabm = 282270001)
                break

    for x in client.get_data(type="leads")['value']:
        if x['fullname'] == fullname:
            print("Values now:", x['leadid'], x['fullname'], x['statecode'], x['statuscode'], x['axx_aprobacionabm'])
            break

    if UserSaidYes("\r\nAgain?"):
        continue
    else:
        break
