# Génération procédurale de terrain et analyse de l'efficacité

## Génération procédurale
**Génération**
Dans le cadre du projet, plusieurs algorithmes sont implémentés pour avoir la capacité de modifier à sa guise un terrain généré. Par exemple, du *Fractal Brownian Motion* (FBM) ainsi que du Voronoi.
Des outils tels que l'addition et la multiplication de *height map* sont implémentés pour atteindre une complexité et une précision de génération autrement inimaginable.

**Érosion**
D'autre part, des algorithmes additionnels peuvent être utilisés pour modifier le terrain généré pour ajouter du réalisme. L'ajout d'érosion hydraulique, d'érosion fluvials et d'érosion thermique sont des facteurs clés quant à un terrain représentatif de la réalité.

## Interface graphique
Un système de nodes permet également de contrôler la génération, en liant par exemple des générateurs de FBM à un algorithme d'érosion.

## Analyse des résultats
Les critères d'analyse sont d'abord la rapidité d'exécution, car ceci est une étude des meilleurs algorithmes dans les jeux vidéos dans l'optique des les classer. 
Le critère de rapidité n'est par contre pas le seul. Dans des jeux vidéo où le terrain est préalablement généré, il est possible d'utiliser de l'érosion sur des grandes surfaces, car cela n'affectera pas les performances après que le terrain soit généré.
Ce deuxième critère est donc le visuel, c'est-à-dire à quel point le terrain généré sera beau et réaliste pour les joueurs. Ce critère est complètement séparé mais, après avoir analysé les résultats, il sera possible de trouver la combinaison d'algorithmes qui donnent le meilleur compromis.

## Ouverture du projet
Le projet a été créé dans la version de **Unity 6000.3.5f2** et nécessite cette version pour être ouvert.
