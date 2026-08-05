# Frozen page captures for HtmlToGum canaries.
#
# Create with:
#   npm run freeze -- https://developer.mozilla.org/en-US/docs/Web/CSS/background-size --id=mdn-background-size
#   npm run freeze -- https://docs.python.org/3/ --id=python-docs-3
#
# Each folder contains index.html (with <base href> to origin), meta.json, and a
# chromium-reference.png. Relative assets still fetch from the live origin.
