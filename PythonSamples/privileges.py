
from dynamics365crm.client import Client
import dynamics365ce_helpers

def Query(entity, fields):
	records = client.get_data(type=entity)
	for r in records['value']:
		for f in fields:
			print(r[f], sep=' ', end='')
		print()

resource = input("Dynamics 365 URL? ")
token = dynamics365ce_helpers.GetInteractiveToken(resource = resource)
client = Client(resource, token=token)

Query("privileges", ['name', 'accessright'])