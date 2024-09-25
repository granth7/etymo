from swiftshadow import QuickProxy
from googlesearch import search
from random import randint
from time import sleep

swift = QuickProxy()

def getProxyUrl():
    proxy = swift
    del proxy[-1]
    proxyUrl = 'http' + '://' + proxy[0] + '/'

    return proxyUrl

def slicer(my_str,sub):
   index=my_str.find(sub)
   if index !=-1 :
         return my_str[index:] 
   else :
         return my_str

def getMorphemeDescriptions(morpheme: str="", proxyUrl: str=""):
    term = 'etymonline.com ' + morpheme

    searchResults = search(term, num_results=3, lang="en", proxy=proxyUrl, advanced=True, sleep_interval=10)

    for result in searchResults:
        if ('etymonline' in result.url) and (f'/word/{morpheme}-' in result.url) and not('The online etymology dictionary' in result.description):
            # Trim result description
            tempString = slicer(result.description,'— ')
            trimmedDescription = tempString.replace('— ', '')
            trimmedDescription = trimmedDescription.removesuffix('\xa0...')

            return trimmedDescription

morphemes = ['mono','ty','juxta','auto','ex','sym','mis','vas','al','er','pre','over','de','cine','at','dec','cryo','pur','epi','e','non','out','be','min','god','syn','off','poly','cross','quadro','o','pros','es','fore','ob','ante','inter','re','quad','in','retro','under','deca','mid','ed','on','muli','dys','sur','sub','centi','cata','eu','intro','steen','ec','after','infra','quint','dia','con','com','pro','pos','see','glou','ambi','ful','trans','di','extra','u','aes','up','ant','half','su','hexa','macro','duo','kilo','milli','pan','semi','omni','post','im','letra','un','so','hepta','peri','hemi','by','per','ad','du','en','sel','ab','tetra','hypo','melli','gor','ana','uni','super','back','sy','micro','circum','co','ef','septa','demi','mega','dis','sus','multi','a','mal','wo','se','down','thru','through','intra','dun','pent','tel','anti','penta','octo','hyper','ultra','meta','sexa','bi','tri','pano','mac','pel']


# Create a dictionary to store words and their definitions
morpheme_definitions = {}

rotatedProxyUrl = getProxyUrl()
for morpheme in morphemes:
        morpheme_definitions[morpheme] = getMorphemeDescriptions(morpheme=morpheme, proxyUrl=rotatedProxyUrl)
        sleep(randint(30,40))

with open("prefixes.json", "a") as f:
    print(morpheme_definitions)
print('done.')
