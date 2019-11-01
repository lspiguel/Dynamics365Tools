import adal
import json

'''
Usage:
token = GetToken(
                resource_url = "<Dynamics 365 URL>",
                client_id = "51f81489-12ee-4a9e-aaae-a2591f45987d",
                username = "<your username>",
                password = "<your password>"
                )
'''
def GetToken(resource_url, client_id, username, password):
	authority_url = "https://login.microsoftonline.com/common"
	context = adal.AuthenticationContext(authority_url,
		validate_authority = True,
		api_version = None)
	token = context.acquire_token_with_username_password(resource = resource_url,
		username = username,
		password = password,
		client_id = client_id)
	access_token = token['accessToken']
	return access_token

def AskForCredentials():
	credentials = dict()
	credentials['username'] = input('Username? ')
	credentials['password'] = input('Password? ')
	return credentials

def GetInteractiveToken(resource, persist=True, client_id="51f81489-12ee-4a9e-aaae-a2591f45987d"):
	if persist == True:
		try:
			with open('credentials.txt') as f:
				resources = json.load(f)
			if resource in resources:
				credentials = resources[resource]
			else:
				credentials = AskForCredentials()
		except:
			resources = {}
			credentials = AskForCredentials()
	else:
		credentials = AskForCredentials()
	try:
		token = GetToken(resource,
			client_id,
			credentials['username'],
			credentials['password'])
		credentials['lasttoken'] = token
		if persist == True:
			resources[resource] = credentials
			with open('credentials.txt', 'w') as f:
				json.dump(resources, f)
		return token
	except:
		return ''
