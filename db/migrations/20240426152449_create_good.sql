-- migrate:up

CREATE TABLE public.events_01_good (
                                          id integer NOT NULL,
                                          aggregate_id uuid NOT NULL,
                                          event text NOT NULL,
                                          published boolean NOT NULL DEFAULT false,
                                        --   kafkaoffset BIGINT,
                                        --   kafkapartition INTEGER,
                                          "timestamp" timestamp without time zone NOT NULL
);

ALTER TABLE public.events_01_good ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.events_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);

CREATE SEQUENCE public.snapshots_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE TABLE public.snapshots_01_good (
                                             id integer DEFAULT nextval('public.snapshots_01_good_id_seq'::regclass) NOT NULL,
                                             snapshot text NOT NULL,
                                             event_id integer, -- the initial snapshot has no event_id associated so it can be null
                                             aggregate_id uuid NOT NULL,
                                             aggregate_state_id uuid,
                                             "timestamp" timestamp without time zone NOT NULL
);

ALTER TABLE ONLY public.events_01_good
    ADD CONSTRAINT events_good_pkey PRIMARY KEY (id);

ALTER TABLE ONLY public.snapshots_01_good
    ADD CONSTRAINT snapshots_good_pkey PRIMARY KEY (id);

ALTER TABLE ONLY public.snapshots_01_good
    ADD CONSTRAINT event_01_good_fk FOREIGN KEY (event_id) REFERENCES public.events_01_good (id) MATCH FULL ON DELETE CASCADE;

CREATE SEQUENCE public.aggregate_events_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE TABLE public.aggregate_events_01_good (
                                                    id integer DEFAULT nextval('public.aggregate_events_01_good_id_seq') NOT NULL,
                                                    aggregate_id uuid NOT NULL,
                                                    aggregate_state_id uuid,
                                                    event_id integer
);

ALTER TABLE ONLY public.aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_good_pkey PRIMARY KEY (id);

ALTER TABLE ONLY public.aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_fk  FOREIGN KEY (event_id) REFERENCES public.events_01_good (id) MATCH FULL ON DELETE CASCADE;

CREATE OR REPLACE FUNCTION insert_01_good_event_and_return_id(
    IN event_in text,
    IN aggregate_id uuid
)
RETURNS int
       
LANGUAGE plpgsql
AS $$
DECLARE
inserted_id integer;
BEGIN
INSERT INTO events_01_good(event, aggregate_id, timestamp)
VALUES(event_in::text, aggregate_id, now()) RETURNING id INTO inserted_id;
return inserted_id;
END;
$$;

CREATE OR REPLACE FUNCTION insert_01_good_aggregate_event_and_return_id(
    IN event_in text,
    IN aggregate_id uuid 
    -- in aggregate_state_id uuid
)
RETURNS int
    
LANGUAGE plpgsql
AS $$
DECLARE
inserted_id integer;
    event_id integer;
BEGIN
    event_id := insert_01_good_event_and_return_id(event_in, aggregate_id);

-- INSERT INTO aggregate_events_01_good(aggregate_id, event_id, aggregate_state_id )
-- VALUES(aggregate_id, event_id, aggregate_state_id) RETURNING id INTO inserted_id;

INSERT INTO aggregate_events_01_good(aggregate_id, event_id)
VALUES(aggregate_id, event_id) RETURNING id INTO inserted_id;

return event_id;
END;
$$;

CREATE OR REPLACE PROCEDURE set_classic_optimistic_lock_01_good() AS $$
BEGIN 
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'aggregate_events_01_good_aggregate_id_state_id_unique') THEN
ALTER TABLE aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_good_aggregate_id_state_id_unique UNIQUE (aggregate_state_id);
END IF;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE un_set_classic_optimistic_lock_01_good() AS $$
BEGIN
    ALTER TABLE aggregate_events_01_good
    DROP CONSTRAINT IF EXISTS aggregate_events_01_good_aggregate_id_state_id_unique; 
    -- You can have more SQL statements as needed
END;
$$ LANGUAGE plpgsql;




-- migrate:down
