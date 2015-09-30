Description
--------
The Japanese are infinitely in love with machinery that surrounds them. They follow closely all technical innovations and try to use the most modern and clever of them. Den and Sergey have an ingenious plan: they want to create a text editor that will win the Japanese over. The most important feature of the editor will be the autocompletion function. If a user has typed first several letters of a word, then the editor will automatically suggest the most probable endings.
Den and Sergey have collected a lot of Japanese texts. For each Japanese word they counted the number of times it was found in the texts. For the first several letters entered by a user, the editor must show no more than ten words starting with these letters that are most commonly used. These words will be arranged in the order of decreasing encounter frequencies.
Help Sergey and Den to turn over the market of text editors.

Input
--------
The first line contains the number of words found in the texts N (1 ? N ? 105). Each of the following N lines contains a word wi and an integer ni separated with a space, where wi is a nonempty sequence of lowercase Latin letters no longer than 15 symbols, and ni (1 ? ni ? 106) is the number of times this word is encountered in the texts. The (N + 2)th line contains a number M (1 ? M ? 15000). In each of the next M lines there is a word ui (a nonempty sequence of lowercase Latin letters no longer than 15 symbols), which is the beginning of a word entered by a user.

Output
--------
For each of the M lines, output the most commonly used Japanese words starting with ui in the order of decreasing encounter frequency. If some words have equal frequencies, sort them lexicographically. If there are more than ten different words starting with the given sequence, output the first ten of them. The lists of words for each ui must be separated by an empty line.