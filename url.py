import io
import os
import urllib2
from bs4 import BeautifulSoup

cleanIngredients = []
url = "http://www.recipepuppy.com/?i="
ingList = open("query.txt", "r")


with open('query.txt', 'r') as query:
    rawIngredients = [line.strip() for line in query]

#print rawIngredients
uniqueQuery = list(set(rawIngredients))
    
#print uniqueQuery
for i in uniqueQuery:
    if len(i)>0:
        item = i + '%2C+'
        cleanIngredients.append(item)
#print cleanIngredients

#with open(ingList) as f_in:
#   lines = filter(None, (line.rstrip() for line in f_in))

final = ''.join(cleanIngredients)
#print final
#webbrowser.open_new(url + final)

webScrape = open("webScrape.txt", "w+")

soup = BeautifulSoup(urllib2.urlopen(url+final).read())

divTag = soup.find_all('div', {'class':'result'})

for tag in divTag:
    #print('Recipe:')
    title = tag.find_all('h3')
    for titleItem in title:
        #print('Recipe Title: ')
        tempTitle = titleItem.text.strip()
        break
    link = tag.find_all('a')
    for linkItem in link:
        #print('Link: ')
        tempLink = linkItem.get('href')
        break
    image = tag.find_all('img', {'class': 'thumb'})
    for imageItem in image:
        #print('Image URL: ')
        tempImg = imageItem.get('src')
        break
    webScrape.write(tempTitle+'*'+tempLink+'*'+tempImg+'\n')

