from googlesearch import search
#from swiftshadow.classes import Proxy
from swiftshadow import QuickProxy
from urllib.error import HTTPError
from random import randint
from time import sleep

#swift = Proxy(countries=['US'], autoRotate=True, cachePeriod=2)
swift = QuickProxy()

def getRotatedProxyUrl():
    # proxy = swift.proxy()
    proxy = swift

    del proxy[-1]
    rotatedUrl = 'http' + '://' + proxy[0] + '/'
    return rotatedUrl

def getMorphemeDescriptions(morpheme: str="", proxyUrl: str=""):
    term = 'etymonline.com ' + morpheme

    try:   
        searchResults = search(term, num_results=1, lang="en", proxy=proxyUrl, advanced=True)
    except HTTPError as err:
        if err.code == 429:
            proxyUrl = getRotatedProxyUrl()
        else:
            raise
    #finally:
    #    proxyUrl = getRotatedProxyUrl()
    #    searchResults = search(term, num_results=1, lang="en", proxy=proxyUrl, advanced=True)

    for result in searchResults:
        if 'etymonline' in result.url:
            # For now, return on the first result. May rework later to return the best definition.
            return morpheme + ': ' + result.description

# List of morphemes you want to get definitions for

morphemes = ['mono','ty','juxta','auto','ex','sym','mis','vas','al','er','pre','over','de','cine','at','dec','cryo','pur','epi','e','non','out','be','min','god','syn','off','poly','cross','quadro','o','pros','es','fore','ob','ante','inter','re','quad','in','retro','under','deca','mid','ed','on','muli','dys','sur','sub','centi','cata','eu','intro','steen','ec','after','infra','quint','dia','con','com','pro','pos','see','glou','ambi','ful','trans','di','extra','u','aes','up','ant','half','su','hexa','macro','duo','kilo','milli','pan','semi','omni','post','im','letra','un','so','hepta','peri','hemi','by','per','ad','du','en','sel','ab','tetra','hypo','melli','gor','ana','uni','super','back','sy','micro','circum','co','ef','septa','demi','mega','dis','sus','multi','a','mal','wo','se','down','thru','through','intra','dun','pent','tel','anti','penta','octo','hyper','ultra','meta','sexa','bi','tri','pano','mac','pel']

# Create a dictionary to store words and their definitions
morpheme_definitions = {}

rotatedProxyUrl = getRotatedProxyUrl()
for morpheme in morphemes:
        #rotatedProxyUrl = getRotatedProxyUrl()
        morpheme_definitions[morpheme] = getMorphemeDescriptions(morpheme=morpheme, proxyUrl=rotatedProxyUrl)

        sleep(randint(30,40))

print(morpheme_definitions)










#for word in words:
''' Using nltk:
synsets = wn.synsets(word)
if synsets:
    # Get the first definition for the word
    definition = synsets[0].definition()
    word_definitions[word] = definition
else:
    word_definitions[word] = 'No definition found'
'''



    # Search for definition using Google
