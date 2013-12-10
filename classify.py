import libsvm
import argparse
from cPickle import load
from learn import extractSift, computeHistograms, writeHistogramsToFile

HISTOGRAMS_FILE = 'testdata.svm'
CODEBOOK_FILE = 'codebook.file'
MODEL_FILE = 'trainingdata.svm.model'
queryList = []

def parse_arguments():
    parser = argparse.ArgumentParser(description='classify images with a visual bag of words model')
    parser.add_argument('-c', help='path to the codebook file', required=False, default=CODEBOOK_FILE)
    parser.add_argument('-m', help='path to the model  file', required=False, default=MODEL_FILE)
    parser.add_argument('input_images', help='images to classify', nargs='+')
    args = parser.parse_args()
    return args


print "---------------------"
print "## extract Sift features"
all_files = []
all_files_labels = {}
all_features = {}

args = parse_arguments()
model_file = args.m
codebook_file = args.c
fnames = args.input_images
all_features = extractSift(fnames)
for i in fnames:
    all_files_labels[i] = 0  # label is unknown
    print all_files_labels

print "---------------------"
print "## loading codebook from " + codebook_file
with open(codebook_file, 'rb') as f:
    codebook = load(f)

print "---------------------"
print "## computing visual word histograms"
all_word_histgrams = {}
for imagefname in all_features:
    word_histgram = computeHistograms(codebook, all_features[imagefname])
    all_word_histgrams[imagefname] = word_histgram

print "---------------------"
print "## write the histograms to file to pass it to the svm"
nclusters = codebook.shape[0]
writeHistogramsToFile(nclusters,
                      all_files_labels,
                      fnames,
                      all_word_histgrams,
                      HISTOGRAMS_FILE)

print "---------------------"
print "## test data with svm"
resultLabel = libsvm.test(HISTOGRAMS_FILE, model_file)
print ('THIS IS THE LABEL')
ingList = open(r"C:\Users\Hannah Chen\Documents\Visual Studio 2013\Projects\ColorBasics-WPF\bin\Debug\query.txt", "a")
#url = "http://www.recipepuppy.com/?i="
res = resultLabel
print res
l = ['0','1','2','3','4','5','6','7','8','9']
st_res = 0
for ch in res:
 if ch not in l:
  st_res+=ch
with open('nameList.txt', 'r') as f:
    count = 0;
    for item in f:
        print "COUNT IS: "
        print count
        if count == st_res:
            resultName = item
            break
        else:
            count = count + 1
        
  #  for _ in itertools.islice(f, 0, res-1):
   #     pass
   # for line in itertools.islice(f, 0, res):
    #    resultName = line
print ('I see a(n) ' + resultName)
print>>ingList, resultName
##############################################
#webbrowser.open_new(url + resultName + '%2C+')
