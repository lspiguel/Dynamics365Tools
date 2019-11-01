
import os
import json
from urllib.parse import urlparse, parse_qs
from dynamics365crm.client import Client

import GetD365Token

resource = input("Dynamics 365 URL? ")

token = GetD365Token.GetInteractiveToken(resource = resource)

client = Client(resource, token=token)

contacts = client.get_data(type="contacts")

for x in contacts['value']:
    print(x['fullname'])

