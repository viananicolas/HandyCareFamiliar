﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FreshMvvm;
using HandyCareFamiliar.Data;
using HandyCareFamiliar.Helper;
using HandyCareFamiliar.Model;
using PropertyChanged;
using Syncfusion.SfCalendar.XForms;
using Xamarin.Forms;

namespace HandyCareFamiliar.PageModel
{
    [ImplementPropertyChanged]
    public class ListaAfazerPageModel : FreshBasePageModel
    {
        //private readonly IConclusaoAfazerRestService _conclusaoRestService;

        private bool _finalizarAfazer;
        //private readonly IAfazerRestService _restService;
        //private readonly IPacienteFamiliarRestService _PacienteFamiliarRestService;

        private Afazer _selectedAfazer;
        private Paciente _selectedPaciente;
        //private Afazer _selectedAfazerConcluido;
        public bool AfazerConcluido;
        public Task check;
        public bool deleteVisible;

        public PageModelHelper oHorario { get; set; }
        public PacienteFamiliar PacienteFamiliar { get; set; }
        //public Paciente oPaciente { get; set; }
        public ObservableCollection<Afazer> Afazeres { get; set; }
        public ObservableCollection<Afazer> ConcluidosAfazeres { get; set; }
        public CalendarEventCollection DataRealizacaoAfazeres { get; set; }

        public ObservableCollection<ConclusaoAfazer> AfazeresConcluidos { get; set; }
        public Afazer AfazerSelecionado { get; set; }

        public Command AddAfazer
        {
            get
            {
                return new Command(async () =>
                {
                    deleteVisible = false;
                    var x = new Tuple<Afazer, Paciente, PacienteFamiliar>(null, oPaciente, PacienteFamiliar);
                    await CoreMethods.PushPageModel<AfazerPageModel>(x);
                });
            }
        }

        public Afazer SelectedAfazer
        {
            get { return _selectedAfazer; }
            set
            {
                _selectedAfazer = value;
                if (value != null)
                {
                    AfazerSelected.Execute(value);
                    SelectedAfazer = null;
                }
            }
        }

        public Command<Afazer> AfazerSelected
        {
            get
            {
                return new Command<Afazer>(async afazer =>
                {
                    deleteVisible = true;
                    var a = AfazerSelecionado;
                    RaisePropertyChanged("IsVisible");
                    var x = new Tuple<Afazer, Paciente>(afazer, oPaciente);
                    await CoreMethods.PushPageModel<AfazerPageModel>(x);
                    afazer = null;
                });
            }
        }

        public bool FinalizarAfazer
        {
            get { return _finalizarAfazer; }
            set
            {
                _finalizarAfazer = value;
                var a = AfazerSelecionado;
                AfazerConcluido = true;
                AfazerFinalizado.Execute(value);
            }
        }

        public Command<Afazer> AfazerFinalizado
        {
            get
            {
                return new Command<Afazer>(async afazer =>
                {
                    var a = afazer;
                    await CoreMethods.PushPageModel<AfazerPageModel>(afazer);
                    afazer = null;
                });
            }
        }

        public Paciente oPaciente
        {
            get { return _selectedPaciente; }
            set
            {
                _selectedPaciente = value;
                if (value != null)
                {
                    //ShowMedicamentos.Execute(value);
                    //SelectedPaciente = null;
                }
            }
        }
        public Command VisualizarValidacao
        {
            get
            {
                return
                    new Command(
                        async () =>
                        {
                            await CoreMethods.PushPageModel<ListaAfazeresValidadosPageModel>(PacienteFamiliar);
                        });
            }
        }

        public Command VisualizarConcluidos
        {
            get
            {
                return
                    new Command(
                        async () =>
                        {
                            await CoreMethods.PushPageModel<ListaAfazerConcluidoPageModel>(PacienteFamiliar);
                        });
            }
        }

        public override void Init(object initData)
        {
            base.Init(initData);
            Afazeres = new ObservableCollection<Afazer>();
            oPaciente = new Paciente();
            PacienteFamiliar = new PacienteFamiliar();
            var x = initData as Tuple<Paciente, PacienteFamiliar>;
            if (x != null)
            {
                oPaciente = x.Item1;
                PacienteFamiliar = x.Item2;
            }
        }

        protected override async void ViewIsAppearing(object sender, EventArgs e)
        {
            AfazerSelecionado = new Afazer();
            DataRealizacaoAfazeres = new CalendarEventCollection();
            oHorario = new PageModelHelper {ActivityRunning = true, Visualizar = false, DadoPaciente = true,
                CuidadorExibicao = false};
            await GetAfazeresConcluidos();
            await GetAfazeres();
            oHorario.ActivityRunning = false;
        }

        public async Task GetAfazeresConcluidos()
        {
            try
            {
                await Task.Run(async () =>
                {
                    AfazeresConcluidos =
                        new ObservableCollection<ConclusaoAfazer>(
                            await FamiliarRestService.DefaultManager.RefreshConclusaoAfazerAsync(true));
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task GetAfazeres()
        {
            //INSERIR PACID EM MATERIAL E MEDICAMENTO
            try
            {
                await Task.Run(async () =>
                {
                    oHorario.ActivityRunning = true;
                    oHorario.FinalizarAfazer = false;
                    var selection =
                        new ObservableCollection<Afazer>(
                            await FamiliarRestService.DefaultManager.RefreshAfazerAsync(true));
                    if ((selection.Count > 0) && (AfazeresConcluidos.Count > 0))
                    {
                        var pacresult =
                            new ObservableCollection<PacienteFamiliar>(
                                    await FamiliarRestService.DefaultManager.RefreshPacienteFamiliarAsync(true))
                                .Where(e => e.PacId == oPaciente.Id)
                                .AsEnumerable();
                        var temp = new ObservableCollection<CuidadorPaciente>(
                                    await FamiliarRestService.DefaultManager.RefreshCuidadorPacienteAsync(true))
                                .Where(e => e.PacId == oPaciente.Id)
                                .AsEnumerable();

                        var result = selection.Where(e => !AfazeresConcluidos.Select(m => m.ConAfazer)
                                .Contains(e.Id))
                            .Where(e => temp.Select(m => m.Id).Contains(e.AfaPaciente))
                            .AsEnumerable();
                        foreach (var afazer in result)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                DataRealizacaoAfazeres.Add(new CalendarInlineEvent
                                {
                                    StartTime = afazer.AfaHorarioPrevisto,
                                    EndTime = afazer.AfaHorarioPrevistoTermino,
                                    Subject = afazer.AfaObservacao,
                                    Color = Color.FromHex(afazer.AfaCor),
                                    AutomationId = afazer.Id
                                });
                            });
                        }

                        //Afazeres = new ObservableCollection<Afazer>(result);
                        App.Afazeres = new ObservableCollection<Afazer>(result);
                    }
                    else
                    {
                        foreach (var afazer in selection)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                DataRealizacaoAfazeres.Add(new CalendarInlineEvent
                                {
                                    StartTime = afazer.AfaHorarioPrevisto,
                                    EndTime = afazer.AfaHorarioPrevistoTermino,
                                    Subject = afazer.AfaObservacao,
                                    Color = Color.FromHex(afazer.AfaCor),
                                    AutomationId = afazer.Id
                                });
                            });
                        }

                        //Afazeres = new ObservableCollection<Afazer>(selection);
                    }
                    oHorario.ActivityRunning = false;
                    oHorario.CuidadorExibicao = true;
                    if (Afazeres.Count == 0)
                        oHorario.Visualizar = true;
                });
            }
            catch (ArgumentNullException e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }
        public Command<CalendarEventCollection> AfazeresCalendario
        {
            get
            {
                return new Command<CalendarEventCollection>(afazer =>
                {
                    Afazeres.Clear();
                    oHorario.DadoPaciente = false;
                    foreach (var item in afazer)
                    {
                        Afazeres.Add(new Afazer
                        {
                            AfaHorarioPrevisto = item.StartTime,
                            AfaObservacao = item.Subject,
                            AfaPaciente = item.ClassId,
                            Id = item.AutomationId,
                            AfaHorarioPrevistoTermino = item.EndTime
                        });
                    }
                    oHorario.DadoPaciente = true;
                    var a = Afazeres.Count;
                });
            }
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);
        }

        public override void ReverseInit(object returndData)
        {
            base.ReverseInit(returndData);
            var newAfazer = returndData as Afazer;
            if (!Afazeres.Contains(newAfazer))
            {
                Afazeres.Add(newAfazer);
            }
            //if (AfazerConcluido)
            //{
            //    Task.Run(async () => await GetAfazeresConcluidos());
            //    Task.Run(async () => await GetAfazeres());
            //}
        }

        public void OnItemToggled(object sender, ToggledEventArgs e)
        {
            var toggledSwitch = (ListSwitch) sender;
        }
    }
}