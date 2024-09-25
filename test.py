import requests
from bs4 import BeautifulSoup
import time
import re

def search_google(query):
    url = "https://www.google.com/search?q=" + query
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3"}
    response = requests.get(url, headers=headers)
    return response.text

def get_etymonline_links(html):
    soup = BeautifulSoup(html, 'html.parser')
    links = []
    for a in soup.find_all('a', href=True):
        href = a['href']
        if 'etymonline.com' in href:
            match = re.search(r'/url\?q=(https://www.etymonline.com/[^&]+)', href)
            if match:
                links.append(match.group(1))
    return links

def get_prefix_definition(url):
    response = requests.get(url)
    soup = BeautifulSoup(response.text, 'html.parser')
    definition_section = soup.find('section', class_='word__defination--2q7ZH')
    if definition_section:
        return definition_section.get_text(separator=" ", strip=True)
    return None

def main():
    prefixes = ["a", "ab", "ad"]
    prefix_definitions = {}

    for prefix in prefixes:
        print(f"Searching for prefix: {prefix}")
        html = search_google(f"{prefix} prefix site:etymonline.com")
        links = get_etymonline_links(html)
        
        if not links:
            print(f"No links found for prefix: {prefix}")
            continue
        
        for link in links:
            print(f"Checking link: {link}")
            definition = get_prefix_definition(link)
            if definition:
                prefix_definitions[prefix] = definition
                break
            else:
                print(f"No definition found at: {link}")

        # Sleep to avoid being blocked by Google
        time.sleep(2)

    for prefix, definition in prefix_definitions.items():
        print(f"{prefix}: {definition}")

if __name__ == "__main__":
    main()
