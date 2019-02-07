using Aveva.PDMS.PMLNet;
using Aveva.Pdms.Utilities.CommandLine;
using Aveva.Pdms.Geometry;

[assembly: PMLNetCallable()]
namespace Polymetal.Pdms.Design.DrawListManager
{

    [PMLNetCallable()]
    public class PickPoint
    {
        public delegate void PointSelectedEventHandler(Position pos);  //Тип делегата нашего события
        private static event PointSelectedEventHandler PointSelectedEvent; //Наше "приватное" событие
        public static event PointSelectedEventHandler PointSelected //Публичное свойство-событие, через которые будут добавляться обработчики к "приватному" событию
        {
            add
            {
                if (PointSelectedEvent == null)
                    Start();  //Если ещё не было обработчиков, то запускаем метод Start()
                else
                {
                    PointSelectedEvent -= value;
                    Start();
                }
                PointSelectedEvent += value;
            }

            remove
            {
                PointSelectedEvent -= value;
            }
        }


        /// Конструктор класса PickPoint
        [PMLNetCallable()]
        public PickPoint() {}

        [PMLNetCallable()]
        public void Assign(PickPoint that) {}

        /// Метод, который будет вызван из PML-я при выборе объекта в PDMS selected point.
        [PMLNetCallable()]
        public void PmlPointSelected(string pos)
        {
            //Если у нас есть обработчики, то запускаем событие.
            if (PointSelectedEvent != null) 
                    PointSelectedEvent(Position.Create(pos));
        }

        public static void Start()
        {
            //Если все прошло успешно, то начинается работа с Event Driver Graphic (EDG).
            RunCommand("!!edgCntrl.remove('MyEDGPacket')");  //Мы удалим из системы пакет, если он уже существует
            RunCommand("!packet = object EDGPACKET()"); //Создаем новый пакет
            RunCommand("!packet.definePosition('Pick Position1')"); //Создаем простой одноступенчатый Pick position с помощью definePosition  и присваиваем title для него
            RunCommand("!packet.description = 'MyEDGPacket'");  // Присваиваем описание данного пакета (читай - Id данного пакета)
            // Для того , что бы  создать экземпляр нашего PMLNetCallable класса, необходимо
            // использовать неймспейс данного класса, а именно "Aveva.Pdms.Examples"
            RunCommand("using namespace 'Polymetal.Pdms.Design.DrawListManager'");
            //Создаем глобальный объект PickPointObject - экземпляр нашего класса
            RunCommand("!!PickPointObject = object PickPoint()");
            //в качестве Action-а для EDG-пакета , будет использоваьб метод PmlPointSelected(x,y,z) нашего класса
            RunCommand("!packet.action = '!!PickPointObject.PmlPointSelected(!this.return[1].position.string())'");
            //Если true - то система удаляет данный пакет, после успешного выполнения всех PICK'ов
            RunCommand("!packet.remove = true");
            //Добавляем пакет в систему
            RunCommand("ID POINT @");
            RunCommand("!!edgCntrl.add(!packet)");
        }

        //Метод для упращения работы с RunInPdms()
        private static bool RunCommand(string command)
        {
            return Command.CreateCommand(command).RunInPdms();
        }
    }
}

