open System.IO
open AngleSharp
open AngleSharp.Dom

let words = File.ReadAllLinesAsync("words.txt") |> Async.AwaitTask |> Async.RunSynchronously

let cambridge word = $"https://dictionary.cambridge.org/dictionary/english/{word}"

type PartOfSpeech =
    | Noun
    | Verb
    | Adjective

type Word =
    { word: string
      partOfSpeech: PartOfSpeech
      phonetic: string
      definition: string
      examples: string array
      synonyms: string array }

let results =
    words
    |> fun x ->
        let document =
            BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(cambridge x)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        document.GetElementsByClassName("di-body")
    |> Seq.toArray
    |> Array.map (fun result ->

        let definition =
            result.GetElementsByClassName "ddef_h"
            |> Seq.tryHead
            |> Option.map (fun div -> div.TextContent |> fun def -> def.Replace(":", ""))
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

        { definition = definition
          partOfSpeech = partOfSpeech
          phonetic = phonetic
          examples = examples
          synonyms = [| synonym |]
          word = "" })

printfn $"%A{results}"
