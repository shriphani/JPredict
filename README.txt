JPredict is a tiny experiement in handwritten input. I wanted to implement a simple classifier to predict Hiragana symbols as they were drawn. 

For building a training set, compile the JPredict project in VS and run it. Feel free to mail me with questions.

The classifier used is a Nearest-Neighbors classifier with stroke-start-point, centroid, and stroke-end-point as the features. The performace is very good with 1 data-item / symbol (so this is a viable approach for 1-shot hiragana recognition).
