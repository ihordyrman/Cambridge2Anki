open System.IO
open AngleSharp
open AngleSharp.Dom

let words = File.ReadAllLinesAsync("words.txt") |> Async.AwaitTask |> Async.RunSynchronously

let cambridge word = $"https://dictionary.cambridge.org/dictionary/english/{word}"

type PartOfSpeech =
    | Noun
    | Verb
    | Adjective
    | Adverb
    | PhrasalVerb

type Word =
    { word: string
      partOfSpeech: PartOfSpeech
      phonetic: string
      definition: string
      image: string option
      examples: string array
      synonyms: string array }

let results =
    words
    |> Array.map (fun word ->
        let document =
            BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(cambridge word)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let result = document.GetElementsByClassName("di-body") |> Seq.head

        let definition =
            result.GetElementsByClassName "ddef_h"
            |> Seq.tryHead
            |> Option.map (fun div -> div.TextContent |> fun def -> def.Replace(":", "").Trim())
            |> Option.defaultValue ""

        let partOfSpeech =
            result.GetElementsByClassName "pos dpos"
            |> Seq.tryHead
            |> Option.map (fun span -> span.TextContent)
            |> fun span ->
                match span with
                | None -> failwith "Part of speech not found"
                | Some "noun" -> PartOfSpeech.Noun
                | Some "verb" -> PartOfSpeech.Verb
                | Some "adjective" -> PartOfSpeech.Adjective
                | Some "adverb" -> PartOfSpeech.Adverb
                | Some "phrasal verb" -> PartOfSpeech.PhrasalVerb
                | _ -> failwith "Unknown part of speech"

        let phonetic =
            result.GetElementsByClassName "uk dpron-i "
            |> Seq.tryHead
            |> Option.map (fun (x: IElement) -> x.GetElementsByClassName "pron dpron")
            |> fun x ->
                match x with
                | None -> ""
                | Some x -> x |> Seq.tryHead |> Option.map (fun span -> span.TextContent) |> Option.defaultValue ""

        let examples =
            result.GetElementsByClassName "examp dexamp"
            |> Seq.map (fun x -> x.TextContent.Trim())
            |> Seq.toArray


        let synonym =
            result.GetElementsByClassName "x-h dx-h"
            |> Seq.tryHead
            |> Option.map (fun x -> x.TextContent)
            |> Option.defaultValue ""

        let image =
            result.GetElementsByTagName "amp-img"
            |> Seq.tryHead
            |> Option.map (fun x -> "https://dictionary.cambridge.org/" + x.GetAttribute("src"))

        { definition = definition
          partOfSpeech = partOfSpeech
          phonetic = phonetic
          examples = examples
          synonyms = [| synonym |]
          word = word
          image = image })

for result in results do
    printfn $"%A{result}"
