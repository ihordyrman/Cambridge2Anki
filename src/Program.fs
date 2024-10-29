open System
open System.IO
open System.Text
open System.Threading.Tasks
open AngleSharp
open AngleSharp.Dom

[<Literal>]
let deck = "English::Vocabulary"

let cambridge word = $"https://dictionary.cambridge.org/dictionary/english/{word}"
let words = File.ReadAllLinesAsync("words.txt") |> Async.AwaitTask |> Async.RunSynchronously

type PartOfSpeech =
    | Noun
    | Verb
    | Adjective
    | Adverb
    | PhrasalVerb

type Word =
    { word: string
      cloze: string
      partOfSpeech: PartOfSpeech
      phonetic: string
      definition: string
      image: string option
      examples: string array
      synonyms: string array }

let sb = StringBuilder()
sb.Append("#separator:tab" + Environment.NewLine) |> ignore
sb.Append("#html:false" + Environment.NewLine) |> ignore
sb.Append("#deck column:1" + Environment.NewLine) |> ignore

let results =
    words
    |> Array.map (fun word ->
        let document =
            BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(cambridge word)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        let cloze =
            match word.Length with
            | 0 -> "_"
            | x when x < 4 -> "_" + word.Substring(1)
            | 4 -> word.Substring(0, 1) + "__"
            | _ -> word.Substring(0, 1) + "__" + word.Substring(word.Length - 2)

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

        // after many requests, the website starts to block the requests
        async { return! Task.Delay 1000 |> Async.AwaitTask } |> Async.RunSynchronously

        { definition = definition
          cloze = cloze
          partOfSpeech = partOfSpeech
          phonetic = phonetic
          examples = examples
          synonyms = [| synonym |]
          word = word
          image = image })
    |> Array.iter (fun word ->
        sb.Append(deck + "\t") |> ignore
        sb.Append(word.word + "\t") |> ignore
        sb.Append(word.cloze + "\t") |> ignore
        sb.Append(word.phonetic + "\t") |> ignore
        sb.Append("[replace-me]\t") |> ignore // audio will be taken from anki app via azure api
        sb.Append(word.definition + "\t") |> ignore
        sb.Append(word.definition + "\t") |> ignore
        sb.Append((word.examples |> String.concat ", ") + "\t") |> ignore
        sb.Append((word.image |> (Option.defaultValue "[replace-me]")) + "\t") |> ignore
        sb.Append((word.synonyms |> String.concat ", ") + "\t") |> ignore
        sb.Append("\n") |> ignore)

File.WriteAllText($"anki-{DateTime.Now.ToShortDateString()}.txt", sb.ToString())
