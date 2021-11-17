
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
fullname = input("fullname?")

while True:
    if UserSaidYes("Update?"):
        for x in client.get_data(type="leads")['value']:
            if x['fullname'] == fullname:
                print("Old values:",
											x['leadid'],
											x['fullname'],
											x['statecode'],
											x['statuscode'],
											x['axx_estadodesolicitudaltacrm'],
											x['axx_mensajedesolicitudaltacrm'],
											x['axx_codigonuevoclientealtacrm'])
                client.update_lead(x['leadid'],
																	 axx_estadodesolicitudaltacrm = '66',
																	 axx_mensajedesolicitudaltacrm = 'De nuevo ha ocurrido un error indeseado',
																	 axx_codigonuevoclientealtacrm = '')
                break

    for x in client.get_data(type="leads")['value']:
        if x['fullname'] == fullname:
            print("Values now:",
											x['leadid'],
											x['fullname'],
											x['statecode'],
											x['statuscode'],
											x['axx_estadodesolicitudaltacrm'],
											x['axx_mensajedesolicitudaltacrm'],
											x['axx_codigonuevoclientealtacrm'])
            break

    if UserSaidYes("\r\nAgain?"):
        continue
    else:
        break
