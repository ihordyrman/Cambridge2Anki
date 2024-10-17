open System.IO
open FSharp.Data

let words = File.ReadAllLinesAsync("words.txt") |> Async.AwaitTask |> Async.RunSynchronously

let thesaurus word = $"https://www.thesaurus.com/browse/{word}"
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
      examples: string list
      synonyms: string list }


// let resposnse = HtmlDocument.Load(thesaurus "eloquent")
// printfn $"{resposnse}"
// todo: bypass anti-scraping measures

let results =
    words
    |> fun x -> HtmlDocument.Load(cambridge x).Descendants "div"
    |> Seq.filter (fun x -> x.HasClass "di-body")
    |> Seq.take 1
    |> Seq.map (fun x ->
        let definition =
            x.Descendants "div"
            |> Seq.filter (fun div -> div.HasClass "ddef_h")
            |> Seq.tryHead
            |> Option.map (fun div -> div.InnerText() |> fun def -> def.Replace(":", ""))
            |> Option.defaultValue ""

        let partOfSpeech =
            x.Descendants "span"
            |> Seq.filter (fun span -> span.HasClass "pos dpos")
            |> Seq.tryHead
            |> Option.map (fun span -> span.InnerText())
            |> fun span ->
                match span with
                | None -> failwith "Part of speech not found"
                | Some "noun" -> PartOfSpeech.Noun
                | Some "verb" -> PartOfSpeech.Verb
                | Some "adjective" -> PartOfSpeech.Adjective
                | _ -> failwith "Unknown part of speech"

        let phonetic =
            x.Descendants "span"
            |> Seq.filter (fun x -> x.HasClass "uk dpron-i ")
            |> Seq.tryHead
            |> Option.map (fun span -> span.Descendants "span" |> Seq.filter (fun span -> span.HasClass "pron dpron"))
            |> fun x ->
                match x with
                | None -> ""
                | Some x -> x |> Seq.tryHead |> Option.map (fun span -> span.InnerText()) |> Option.defaultValue ""


        printfn $"%s{definition}"
        printfn $"{partOfSpeech}"
        printfn $"%s{phonetic}"

        x.InnerText())
    |> Seq.toArray

printfn $"%i{results.Length}"
