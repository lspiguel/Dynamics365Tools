import os
import time
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

def Query(entity, fields):
	os.system('cls')
	records = client.get_data(type=entity)
	for r in records['value']:
		for f in fields:
			print(r[f], sep=' ', end='')
		print()

resource = input("Dynamics 365 URL? ")
token = dynamics365ce_helpers.GetInteractiveToken(resource = resource)
client = Client(resource, token=token)

while True:
	Query("importfiles", ['name', 'statuscode', 'successcount', 'failurecount', 'totalcount', 'createdon', '_createdby_value'])
	time.sleep(2)
	if UserSaidYes("\r\nAgain?"):
		continue
	else:
		break
