namespace Prelude.Calculator

open System
open Percyqaz.Common
open Prelude
open Prelude.Calculator
open Prelude.Charts

[<Struct>]
type NoteDifficulty =
    {
        mutable J: float32
        mutable SL: float32
        mutable SR: float32
    }

[<Struct>]
type HandDifficulty =
    {
        mutable Row: NoteRow
        
        mutable Stream: float32
        mutable Jack: float32
        mutable Chord: float32
        mutable ChordJack: float32
    }

[<Struct>]
type RowDifficulty =
    {
        mutable Time: Time
        mutable Row: NoteRow
        
        mutable Stream: float32
        mutable Jack: float32
        mutable Chord: float32
        mutable ChordJack: float32
        
        mutable LeftHand: HandDifficulty
        mutable RightHand: HandDifficulty
    }

module NoteDifficulty =

    /// Cutoff of `JACK_CURVE_CUTOFF` prevents stacked minijacks from slingshotting the perceived BPM
    /// Especially since you can typically hit such minijacks with good accuracy by playing them as a 230 jack no matter the BPM
    let JACK_CURVE_CUTOFF = 230.0f
    /// Converts ms difference between notes (in the same column) into its equivalent BPM
    /// (on 1/4 snap, hence 1 minute * 1/4 is numerator)
    let ms_to_jack_bpm (delta: GameplayTime) : float32 =
        15000.0f<ms / rate> / delta
        |> min JACK_CURVE_CUTOFF

    /// These variables adjust what BPM the stream curve cuts off at
    /// Best to put this whole expression into https://www.desmos.com/calculator + variables as sliders if you want to understand it better
    let STREAM_CURVE_CUTOFF = 10.0f
    let STREAM_CURVE_CUTOFF_2 = 10.0f
    /// Converts ms difference between notes (in the same column) into its equivalent BPM (on 1/4 snap)
    ///
    /// Once the ms gets low enough, the curve starts going back down towards 0
    /// since adjacent notes that would make a "500bpm stream" can be hit as grace notes and don't add difficulty
    /// The real threshold is lower than 500 though, and controlled by the cutoff variables
    let ms_to_stream_bpm (delta: GameplayTime) : float32 =
        300.0f / (0.02f<rate / ms> * delta) -
        300.0f / MathF.Pow(0.02f<rate / ms> * delta, STREAM_CURVE_CUTOFF) / STREAM_CURVE_CUTOFF_2
        |> max 0.0f

    /// Consider note A in column 1, note B also in column 1, 100ms earlier and note C in column 2, also 100ms earlier
    /// Note A should not get any "stream" value because you will be using your wrist to tap the key again, right after note B
    ///
    /// This function uses the ratio between the jack and stream spacing to determine a multiplier between 0.0 and 1.0
    /// Example: for [12][1] as described above, 0.0 is returned to fully cancel out the stream value
    /// Example: for [1][2][1] evenly spaced trill (note C is 50ms earlier instead), 1.0 is returned to do no cancelling at all
    /// Best to put this whole expression into https://www.desmos.com/calculator if you want to understand it better
    let jack_compensation (jack_delta: GameplayTime) (stream_delta: GameplayTime) : float32 =
        let ratio = jack_delta / stream_delta
        MathF.Log(ratio, 2.0f)
        |> max 0.0f
        |> sqrt
        |> min 1.0f

    /// Walks forward through each row of notes in a chart:
    /// - Sets J ("jack") on each note to the BPM between it and the previous note in its column
    /// - Sets SL ("stream-left") on each note to the BPM betwen it and the previous note in the column to the left, if this column exists and is on the same hand
    /// - Sets SR ("stream-right") on each note to the BPM betwen it and the previous note in the column to the right, if this column exists and is on the same hand
    /// For higher keymodes, SL and SR get the maximum value out of all left- and right-notes respectively
    let calculate_note_ratings (rate: Rate, notes: TimeArray<NoteRow>) : NoteDifficulty array array =
        let keys = notes.[0].Data.Length
        let data = Array.init notes.Length (fun _ -> Array.zeroCreate keys)
        let hand_split = Layout.keys_on_left_hand keys

        let last_note_in_column = Array.create<Time> keys ((TimeArray.first notes).Value.Time - 1000000.0f<ms>)

        let note_difficulty (i: int, k: int, time: Time) =
            let jack_delta =
                let delta = (time - last_note_in_column.[k]) / rate
                data.[i].[k].J <- ms_to_jack_bpm delta
                delta

            let hand_lo, hand_hi =
                if k < hand_split then
                    0, hand_split - 1
                else
                    hand_split, keys - 1

            let mutable sl = 0.0f
            let mutable sr = 0.0f
            for hand_k = hand_lo to hand_hi do
                if hand_k <> k then
                    let trill_delta = (time - last_note_in_column.[hand_k]) / rate
                    let trill_v = ms_to_stream_bpm trill_delta * jack_compensation jack_delta trill_delta
                    if hand_k < k then
                        sl <- max sl trill_v
                    else
                        sr <- max sr trill_v

            data.[i].[k].SL <- sl
            data.[i].[k].SR <- sr

        for i = 0 to notes.Length - 1 do
            let { Time = time; Data = nr } = notes.[i]

            for k = 0 to keys - 1 do
                if nr.[k] = NoteType.NORMAL || nr.[k] = NoteType.HOLDHEAD then
                    note_difficulty (i, k, time)

            for k = 0 to keys - 1 do
                if nr.[k] = NoteType.NORMAL || nr.[k] = NoteType.HOLDHEAD then
                    last_note_in_column.[k] <- time

        data
    
    let calculate_row_ratings (rate: Rate, rows: TimeArray<NoteRow>): RowDifficulty array =
        
        let data = Array.zeroCreate rows.Length
        
        let keys = rows[0].Data.Length
        
        let mutable last_note_in_column: TimeArray<NoteType> = Array.zeroCreate keys
        
        // TODO: take hands in consideration
        let is_left_hand col keys = col < keys / 2
        
        // Handle odd keys :
        // For 7K we do not want the spacebar to be counted as right hand nor left hand, because it depends on the player's choice.
        // Instead, we will count the middle key (potentially spacebar) as a special key
        let is_right_hand col keys = if keys % 2 = 1 then col > keys / 2 else col >= keys / 2
        
        let has_middle_col keys = keys % 2 = 1
        
        let row_difficulty (previous_item: TimeItem<NoteRow> option, current_item: TimeItem<NoteRow>, next_item: TimeItem<NoteRow>) =
            // for each note in the row, we will set a probability for the note to be in part of a specific pattern.
            // Then, the note's score will be based on the current pattern calculation for the note, multiplied by the probability
            
            // So, a note with a jack score of 250, a stream score of 130 and a LN score of 12
            // with a jack probability of 0.8, a stream probability of 0.1 and a LN probability of 0.2 will have a note of
            // 250*0.8 + 130*0.1 + 12*02 = 213.4
            // Pattern detection will work as follows :
            
            // JACK DETECTION :
            // If the previous row has a rice note in the same column as the current note analyzed, then the current note is likely a jack
            // If the delta between previous_time & current_time AND the delta between current_time & next_time are likely the same (+-20bpm), then the current note is likely in the middle of a jack pattern
            // If there is only one note considered as a jack in the row, then we will count the note as a classic jack
            // If there is more than one note considered as a jack in the row, and the next / previous row has different column positions, then we will count the note as a chordjack
            // If there is more than one note considered as a jack in the row, and the next / previous row has the same column positions, then we will count the note as a classic jack
            // Since the first condition has to be checked for the note to be considered as a jack, trills won't be counted as jack patterns, to prevent abuse
            
            // STREAM DETECTION :
            // If the previous row has a rice note at the right / left column of the current note, then the current note is likely a stream
            // If the delta between previous_time & current_time AND the delta between current_time & next_time are likely the same (+-20bpm), then the current note is likely in the middle of a stream pattern
            // If there is more than one note considered as a stream in the row, it means that there is a multiple stream pattern
            // If there is only one note considered as a stream in the row, and there is more than one note in the row, then we will count the notes in the row as a jumpstream/handstream
            //    Else we will count the note as a stream
            
            let previous_time = if previous_item.IsSome then previous_item.Value.Time else 0.0f<ms>
            let current_row = current_item.Data
            let chord_size = (current_row |> Array.filter(fun note -> note <> NoteType.NOTHING)).Length
            
            let time_delta = (current_item.Time - previous_time) / rate
            let stream = if float32 time_delta = 0.0f then 0.0f else 200.0f / float32 time_delta
            
            
            // JACKS
            let mutable jack_count = 0
            let mutable jack = 0.0f
            for i = 0 to current_row.Length - 1 do
                if (current_row[i] = NoteType.NORMAL || current_row[i] = NoteType.HOLDHEAD) then
                    let delta = (current_item.Time - last_note_in_column[i].Time) / rate
                    if delta < 150.0f<ms/rate> then
                        jack_count <- jack_count + 1
                        jack <- jack + ms_to_jack_bpm delta
                    last_note_in_column[i] <- {Time = current_item.Time; Data = current_row[i]}
            
            let chord_diff =
                match chord_size with
                | 1 -> 0.0f
                | 2 -> 2.0f
                | 3 -> 5.0f
                | 4 -> 7.5f
                | x -> float32 x * 2.25f // TODO: Change from linear to logarithmic ?
                
            let chordjack = if jack_count > keys / 2 then chord_diff * float32 jack_count * 0.5f else 0.0f
            
            (current_item.Time, current_row, stream, jack, chordjack, chord_diff)
            
        
        for i = 0 to rows.Length - 1 do
            if i <> 0 then
                let previous_row = rows[i - 1]
                let row = rows[i]
                let time, fullrow, stream, jack, chordjack, chord_diff = row_difficulty(Some previous_row, row)
                data[i].Time <- time
                data[i].Row <- fullrow
                data[i].Stream <- stream
                data[i].Jack <- jack
                data[i].ChordJack <- chordjack
                data[i].Chord <- chord_diff
                
                let left_hand_row = Array.zeroCreate (keys / 2)
                let prev_left_hand_row = Array.zeroCreate (keys / 2)
                let right_hand_row = Array.zeroCreate (keys - (keys / 2))
                let prev_right_hand_row = Array.zeroCreate (keys - (keys / 2))
                
                for k = 0 to keys - 1 do
                    if is_left_hand k keys then
                        left_hand_row[k] <- row.Data[k]
                        prev_left_hand_row[k] <- previous_row.Data[k]
                    else
                        right_hand_row[k % (right_hand_row.Length - 1)] <- row.Data[k]
                        prev_right_hand_row[k % (right_hand_row.Length - 1)] <- previous_row.Data[k]
                        
                let _, _, stream, jack, chordjack, chord_diff = row_difficulty(Some {Time = previous_row.Time; Data = prev_left_hand_row}, {Time = row.Time; Data = left_hand_row})
                data[i].LeftHand <-
                    {
                        Jack = jack
                        Stream = stream
                        Chord = chord_diff
                        ChordJack = chordjack
                        Row = left_hand_row
                    }
                
                let _, _, stream, jack, chordjack, chord_diff = row_difficulty(Some {Time = previous_row.Time; Data = prev_right_hand_row}, {Time = row.Time; Data = right_hand_row})
                data[i].RightHand <-
                    {
                        Jack = jack
                        Stream = stream
                        Chord = chord_diff
                        ChordJack = chordjack
                        Row = right_hand_row
                    }
            else
                let row = rows[i]
                let time, fullrow, stream, jack, chordjack, chord_diff = row_difficulty(None, row)
                data[i].Time <- time
                data[i].Row <- fullrow
                data[i].Stream <- stream
                data[i].Jack <- jack
                data[i].ChordJack <- chordjack
                data[i].Chord <- chord_diff
                
                let left_hand_row = Array.zeroCreate (keys / 2)
                let right_hand_row = Array.zeroCreate (keys - (keys / 2))
                
                for k = 0 to keys - 1 do
                    if is_left_hand k keys then
                        left_hand_row[k] <- row.Data[k]
                    else
                        right_hand_row[k % (right_hand_row.Length - 1)] <- row.Data[k]
                        
                let _, _, stream, jack, chordjack, chord_diff = row_difficulty(None, {Time = row.Time; Data = left_hand_row})
                data[i].LeftHand <-
                    {
                        Jack = jack
                        Stream = stream
                        Chord = chord_diff
                        ChordJack = chordjack
                        Row = left_hand_row
                    }
                
                let _, _, stream, jack, chordjack, chord_diff = row_difficulty(None, {Time = row.Time; Data = right_hand_row})
                data[i].RightHand <-
                    {
                        Jack = jack
                        Stream = stream
                        Chord = chord_diff
                        ChordJack = chordjack
                        Row = right_hand_row
                    }
            
        data

    let OHTNERF = 3.0f
    let STREAM_SCALE = 6f
    let STREAM_POW = 0.5f
    /// Combines all the parts of a note found by `calculate_note_ratings` and creates a single "this note is a bit like X bpm jacks" number
    let total (note: NoteDifficulty) : float32 =
        MathF.Pow(
            MathF.Pow(STREAM_SCALE * note.SL ** STREAM_POW, OHTNERF) +
            MathF.Pow(STREAM_SCALE * note.SR ** STREAM_POW, OHTNERF) +
            MathF.Pow(note.J, OHTNERF),
            1.0f / OHTNERF
        )
        
    
    let V2OHTNERF = 3.0f
    let V2STREAM_SCALE = 5.0f
    let V2STREAM_POW = 0.55f
    let V2CHORD_SCALE = 1.25f
    let V2CHORD_POW = 0.9f
    let V2CHORDJACK_SCALE = 0.85f
    let V2CHORDJACK_POW = 1.0f
    let V2HAND_SCALE = 0.65f
    
    let total_row_difficulty (row: RowDifficulty) : float32 =

        //----------------------------------------
        // COMPONENTS
        //----------------------------------------

        let stream =
            V2STREAM_SCALE
            * (row.Stream ** V2STREAM_POW)

        let chord =
            V2CHORD_SCALE
            * (row.Chord ** V2CHORD_POW)

        let chordjack =
            V2CHORDJACK_SCALE
            * (row.ChordJack ** V2CHORDJACK_POW)

        //----------------------------------------
        // HAND BALANCE
        //----------------------------------------

        // let hand_balance =
        //     1.0f
        //     + abs(row.LeftHand.Total - row.RightHand.Total)
        //         * V2HAND_SCALE
        //         * 0.01f

        //----------------------------------------
        // FINAL
        //----------------------------------------

        MathF.Pow(

            MathF.Pow(stream, V2OHTNERF)
            + MathF.Pow(row.Jack, V2OHTNERF)
            + MathF.Pow(chord, V2OHTNERF)
            + MathF.Pow(chordjack, V2OHTNERF),

            1.0f / V2OHTNERF

        ) // * hand_balance
        
    let total_hand_difficulty (row: HandDifficulty) : float32 =

        //----------------------------------------
        // COMPONENTS
        //----------------------------------------

        let stream =
            V2STREAM_SCALE
            * (row.Stream ** V2STREAM_POW)

        let chord =
            V2CHORD_SCALE
            * (row.Chord ** V2CHORD_POW)

        let chordjack =
            V2CHORDJACK_SCALE
            * (row.ChordJack ** V2CHORDJACK_POW)

        //----------------------------------------
        // FINAL
        //----------------------------------------

        MathF.Pow(

            MathF.Pow(stream, V2OHTNERF)
            + MathF.Pow(row.Jack, V2OHTNERF)
            + MathF.Pow(chord, V2OHTNERF)
            + MathF.Pow(chordjack, V2OHTNERF),

            1.0f / V2OHTNERF

        ) // * hand_balance

type NoteDifficulty with
    member this.Total = NoteDifficulty.total this
    
type RowDifficulty with
    member this.Total = NoteDifficulty.total_row_difficulty this
    
type HandDifficulty with
    member this.Total = NoteDifficulty.total_hand_difficulty this